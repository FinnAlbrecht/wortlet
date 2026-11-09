import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { AuthProvider, useAuth } from "../features/auth/AuthContext";
import { LoginPage } from "../features/auth/LoginPage";
import { FolderEditor } from "../features/folders/FolderEditor";
import { FolderPage } from "../features/folders/FolderPage";
import { LanguagePage } from "../features/languages/LanguagePage";
import { LanguagesPage } from "../features/languages/LanguagesPage";
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
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <div className="wrap">
          <TopBar />
          <AppRoutes />
        </div>
      </BrowserRouter>
    </AuthProvider>
  );
}
