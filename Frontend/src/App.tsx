import { Routes, Route } from 'react-router';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { LoginPage } from './features/auth/pages/LoginPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<div>Home</div>} />
      </Route>
    </Routes>
  )
}