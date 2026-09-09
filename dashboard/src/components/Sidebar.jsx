import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { navSections, gateVisible, itemIsActive } from '../config/navConfig.jsx';

function Sidebar({ onStartTour, onStartWizard }) {
  const { isAdmin, isGlobalAdmin, isTenantAdmin } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const auth = { isAdmin, isGlobalAdmin, isTenantAdmin };
  const isAdminOrTenantAdmin = isGlobalAdmin || isTenantAdmin;

  const logoSrc = '/logo-new-full.png';

  return (
    <nav className="sidebar">
      <div className="sidebar-logo">
        <img src={logoSrc} alt="AssistantHub" />
      </div>
      <div className="sidebar-nav">
        {navSections.map((section) => {
          if (!gateVisible(section.gate, auth)) return null;
          const items = section.items.filter((item) => gateVisible(item.gate, auth));
          if (items.length === 0) return null;
          return (
            <div key={section.key} data-tour-target={section.tourTarget || undefined}>
              <div className="sidebar-section">{section.label}</div>
              {items.map((item) => (
                <button
                  key={item.path}
                  className={`sidebar-item ${itemIsActive(item, location.pathname) ? 'active' : ''}`}
                  onClick={() => navigate(item.path)}
                >
                  {item.icon}
                  <span>{item.label}</span>
                </button>
              ))}
            </div>
          );
        })}
      </div>
      <div className="sidebar-footer">
        <button className="sidebar-footer-link" onClick={onStartTour}>
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
          <span>Take Tour</span>
        </button>
        {isAdminOrTenantAdmin && (
          <button className="sidebar-footer-link" onClick={onStartWizard}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>
            <span>Setup Wizard</span>
          </button>
        )}
      </div>
    </nav>
  );
}

export default Sidebar;
