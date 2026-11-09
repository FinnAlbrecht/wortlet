using System.ComponentModel.DataAnnotations;
using Wortlet.Application.Auth;
using Wortlet.Application.Common;
using ValidationException = Wortlet.Application.Common.ValidationException;
using Wortlet.Domain.Common;
using Wortlet.Domain.Groups;
using Wortlet.Domain.Profiles;

namespace Wortlet.Application.Profiles;

public sealed record ProfileDto(
    Guid UserId, string Email, string DisplayName, string AvatarIcon, string AvatarColor, bool HasImage, long AvatarVersion);

/// <summary>Was andere Gruppenmitglieder von einer Person sehen – keine E-Mail-Adresse.</summary>
public sealed record PublicProfileDto(
    Guid UserId, string DisplayName, string AvatarIcon, string AvatarColor, bool HasImage, long AvatarVersion);

public sealed record AvatarOptionsDto(IReadOnlyList<string> Icons, IReadOnlyList<string> Colors);

public sealed record UpdateProfileRequest(
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(UserProfile.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")] string DisplayName,
    [Required] string AvatarIcon,
    [Required] string AvatarColor);

public sealed record AvatarImageFile(string ContentType, byte[] Data);

public sealed class ProfileService(
    IProfileRepository profiles,
    IGroupRepository groups,
    IIdentityService identity,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public static AvatarOptionsDto Options { get; } = new(UserProfile.Icons, UserProfile.Colors);

    public async Task<ProfileDto> GetMineAsync(CancellationToken ct = default)
    {
        var profile = await EnsureAsync(currentUser.UserId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDto(profile, ct);
    }

    public async Task<ProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var profile = await EnsureAsync(currentUser.UserId, ct);
        Run(() => profile.Update(request.DisplayName, request.AvatarIcon, request.AvatarColor, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDto(profile, ct);
    }

    public async Task<ProfileDto> UploadImageAsync(byte[] data, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var profile = await EnsureAsync(userId, ct);
        var now = clock.UtcNow;
        var image = await profiles.GetImageAsync(userId, ct);
        if (image is null)
            profiles.AddImage(Run(() => AvatarImage.Create(userId, data, now)));
        else
            Run(() => image.Replace(data, now));
        profile.SetHasImage(true, now);
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDto(profile, ct);
    }

    public async Task<ProfileDto> DeleteImageAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var profile = await EnsureAsync(userId, ct);
        if (await profiles.GetImageAsync(userId, ct) is { } image)
            profiles.RemoveImage(image);
        profile.SetHasImage(false, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDto(profile, ct);
    }

    /// <summary>Bild sehen darf man selbst und wer mit einem in einer Gruppe ist.</summary>
    public async Task<AvatarImageFile> GetImageAsync(Guid userId, CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        if (userId != me)
        {
            var mine = await groups.ListForUserAsync(me, ct);
            if (!mine.Any(g => g.IsMember(userId)))
                throw new NotFoundException("Dieses Bild gibt es nicht.");
        }
        var image = await profiles.GetImageAsync(userId, ct) ?? throw new NotFoundException("Dieses Bild gibt es nicht.");
        return new AvatarImageFile(image.ContentType, image.Data);
    }

    /// <summary>Legt das Startprofil an, falls es noch keines gibt (ältere Konten). Speichert nicht.</summary>
    public async Task<UserProfile> EnsureAsync(Guid userId, CancellationToken ct = default)
    {
        if (await profiles.GetAsync(userId, ct) is { } existing)
            return existing;
        var email = await identity.GetEmailAsync(userId, ct)
            ?? throw new AuthenticationFailedException("Dein Konto wurde nicht gefunden. Bitte melde dich neu an.");
        var profile = UserProfile.CreateDefault(userId, email, clock.UtcNow);
        profiles.Add(profile);
        return profile;
    }

    public static PublicProfileDto ToPublic(UserProfile p) =>
        new(p.Id, p.DisplayName, p.AvatarIcon, p.AvatarColor, p.HasImage, p.UpdatedAt.Ticks);

    private async Task<ProfileDto> ToDto(UserProfile p, CancellationToken ct) =>
        new(p.Id, await identity.GetEmailAsync(p.Id, ct) ?? string.Empty, p.DisplayName, p.AvatarIcon, p.AvatarColor, p.HasImage, p.UpdatedAt.Ticks);

    private static T Run<T>(Func<T> action)
    {
        try { return action(); }
        catch (DomainException ex) { throw new ValidationException(ex.Message); }
    }

    private static void Run(Action action) => Run(() => { action(); return 0; });
}
