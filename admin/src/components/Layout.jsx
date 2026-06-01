import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../services/AuthContext.jsx';
import './Layout.css';

export default function Layout() {
  const { session, tenant, logout } = useAuth();
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
          {tenant && <span className="badge info">{tenant.tenantId}</span>}
        </div>
        <nav>
          <NavLink to="/" end>📊 Dashboard</NavLink>
          <NavLink to="/emitir">📄 Emitir CFE</NavLink>
          <NavLink to="/caes">🔑 CAEs</NavLink>
          <NavLink to="/configuracion">⚙️ Configuración</NavLink>
          <NavLink to="/select-tenant">🏢 Cambiar Tenant</NavLink>
        </nav>
        <div className="sidebar-footer">
          <small>{session?.email}</small>
          {tenant && <small className="tenant-name">{tenant.razonSocial}</small>}
          <button className="secondary" onClick={handleLogout}>Cerrar sesión</button>
        </div>
      </aside>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
