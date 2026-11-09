using Wortlet.Domain.Common;
using Wortlet.Domain.Groups;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Domain;

public class GroupVisibilityTests
{
    private static readonly Guid Leader = Guid.NewGuid();
    private static readonly Guid Anna = Guid.NewGuid();
    private static readonly Guid Ben = Guid.NewGuid();

    private static Group NewGroup(GroupVisibility visibility) =>
        Group.Create(Leader, "Klasse 3b", Group.NewJoinCode(), TestData.Now, visibility);

    [Fact]
    public void New_groups_are_private_by_default()
    {
        var g = Group.Create(Leader, "Klasse 3b", Group.NewJoinCode(), TestData.Now);
        Assert.Equal(GroupVisibility.Private, g.Visibility);
        Assert.False(g.IsSearchable);
    }

    [Fact]
    public void Public_groups_can_be_joined_directly()
    {
        var g = NewGroup(GroupVisibility.Public);
        Assert.True(g.JoinFromSearch(Anna, TestData.Now));
        Assert.True(g.IsMember(Anna));
    }

    [Fact]
    public void Private_groups_cannot_be_joined_from_search_but_with_the_code()
    {
        var g = NewGroup(GroupVisibility.Private);
        Assert.Throws<DomainException>(() => g.JoinFromSearch(Anna, TestData.Now));
        g.Join(Anna, TestData.Now);
        Assert.True(g.IsMember(Anna));
    }

    [Fact]
    public void On_request_groups_need_the_leader_to_accept()
    {
        var g = NewGroup(GroupVisibility.OnRequest);
        Assert.False(g.JoinFromSearch(Anna, TestData.Now));
        Assert.False(g.JoinFromSearch(Anna, TestData.Now)); // zweimal anfragen = eine Anfrage
        Assert.Single(g.JoinRequests);
        Assert.False(g.IsMember(Anna));

        Assert.Throws<DomainException>(() => g.AcceptRequest(Anna, Anna, TestData.Now)); // nur die Leitung
        g.AcceptRequest(Leader, Anna, TestData.Now);
        Assert.True(g.IsMember(Anna));
        Assert.Empty(g.JoinRequests);
    }

    [Fact]
    public void Requests_can_be_declined_cancelled_or_replaced_by_the_code()
    {
        var g = NewGroup(GroupVisibility.OnRequest);
        g.JoinFromSearch(Anna, TestData.Now);
        g.DeclineRequest(Leader, Anna);
        Assert.False(g.HasRequested(Anna));
        Assert.Throws<DomainException>(() => g.DeclineRequest(Leader, Anna));

        g.JoinFromSearch(Ben, TestData.Now);
        g.CancelRequest(Ben);
        Assert.Empty(g.JoinRequests);

        g.JoinFromSearch(Anna, TestData.Now);
        g.Join(Anna, TestData.Now); // per Code: Anfrage erledigt
        Assert.True(g.IsMember(Anna));
        Assert.Empty(g.JoinRequests);
    }

    [Fact]
    public void Making_a_group_public_accepts_open_requests()
    {
        var g = NewGroup(GroupVisibility.OnRequest);
        g.JoinFromSearch(Anna, TestData.Now);
        g.JoinFromSearch(Ben, TestData.Now);
        Assert.Throws<DomainException>(() => g.ChangeVisibility(Anna, GroupVisibility.Public, TestData.Now));

        g.ChangeVisibility(Leader, GroupVisibility.Public, TestData.Now);
        Assert.True(g.IsMember(Anna));
        Assert.True(g.IsMember(Ben));
        Assert.Empty(g.JoinRequests);
    }

    [Fact]
    public void Making_a_group_private_keeps_open_requests_for_the_leader()
    {
        var g = NewGroup(GroupVisibility.OnRequest);
        g.JoinFromSearch(Anna, TestData.Now);
        g.ChangeVisibility(Leader, GroupVisibility.Private, TestData.Now);
        Assert.False(g.IsSearchable);
        Assert.True(g.HasRequested(Anna));
    }
}
