import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../services/AuthContext.jsx';
import './Layout.css';

export default function Layout() {
  const { tenant, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-header">
          <h2>UruFactura</h2>
          <span className="badge info">{tenant?.tenantId || 'default'}</span>
        </div>
        <nav>
          <NavLink to="/" end>📊 Dashboard</NavLink>
          <NavLink to="/emitir">📄 Emitir CFE</NavLink>
          <NavLink to="/caes">🔑 CAEs</NavLink>
          <NavLink to="/configuracion">⚙️ Configuración</NavLink>
        </nav>
        <div className="sidebar-footer">
          <small>{tenant?.razonSocial}</small>
          <button className="secondary" onClick={handleLogout}>Cerrar sesión</button>
        </div>
      </aside>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
