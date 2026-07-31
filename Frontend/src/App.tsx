import { Routes, Route } from 'react-router';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { Layout } from './components/Layout';
import { LoginPage } from './features/auth/pages/LoginPage';
import { UsersListPage } from './features/users/pages/UsersListPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/" element={<div>Home</div>} />
          <Route path="/users" element={< UsersListPage />} />
        </Route>
      </Route>
    </Routes>
  )
}