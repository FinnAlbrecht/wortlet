import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { AuthProvider, useAuth } from "../features/auth/AuthContext";
import { LoginPage } from "../features/auth/LoginPage";
import { FolderEditor } from "../features/folders/FolderEditor";
import { FolderPage } from "../features/folders/FolderPage";
import { GroupPage } from "../features/groups/GroupPage";
import { GroupsPage } from "../features/groups/GroupsPage";
import { LanguagePage } from "../features/languages/LanguagePage";
import { LanguagesPage } from "../features/languages/LanguagesPage";
import { ProfileProvider } from "../features/profile/ProfileContext";
import { ProfilePage } from "../features/profile/ProfilePage";
import { SetEditor } from "../features/sets/SetEditor";
import { SetPage } from "../features/sets/SetPage";
import { StatsPage } from "../features/stats/StatsPage";
import { TopBar } from "../shared/ui/TopBar";

function AppRoutes() {
  const { user } = useAuth();
  if (!user)
    return (
      <Routes>
        <Route path="*" element={<LoginPage />} />
      </Routes>
    );
  return (
    <Routes>
      <Route path="/" element={<LanguagesPage />} />
      <Route path="/languages/:languageId" element={<LanguagePage />} />
      <Route path="/languages/:languageId/new" element={<FolderEditor />} />
      <Route path="/folders/:folderId" element={<FolderPage />} />
      <Route path="/folders/:folderId/edit" element={<FolderEditor />} />
      <Route path="/languages/:languageId/sets/new" element={<SetEditor />} />
      <Route path="/sets/:setId" element={<SetPage />} />
      <Route path="/sets/:setId/edit" element={<SetEditor />} />
      <Route path="/stats" element={<StatsPage />} />
      <Route path="/groups" element={<GroupsPage />} />
      <Route path="/groups/:groupId" element={<GroupPage />} />
      <Route path="/profile" element={<ProfilePage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export function App() {
  return (
    <AuthProvider>
      <ProfileProvider>
        <BrowserRouter>
          <div className="wrap">
            <TopBar />
            <AppRoutes />
          </div>
        </BrowserRouter>
      </ProfileProvider>
    </AuthProvider>
  );
}
