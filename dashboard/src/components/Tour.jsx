import React, { useState, useEffect, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { useAuth } from '../context/AuthContext';

const SPOTLIGHT_STEPS = [
  {
    target: 'documents',
    title: 'Documents',
    description: 'Upload and manage documents for your knowledgebases. Documents are summarized, chunked, embedded, and ingested into collections for RAG retrieval.',
  },
  {
    target: 'assistants',
    title: 'Chat & Assistants',
    description: 'Create assistants, configure their settings, review user feedback, and browse conversation history.',
  },
  {
    target: 'artifacts',
    title: 'Artifacts',
    description: 'S3 storage buckets, objects, vector collections, and records from ingested documents.',
    adminOnly: true,
  },
  {
    target: 'configuration',
    title: 'Configuration',
    description: 'Embedding and inference endpoints, ingestion rules, users, credentials, models, and system settings.',
    adminOnly: true,
  },
  {
    target: 'topbar-misc',
    title: 'Toolbar',
    description: 'Server URL badge, admin badge, dark/light mode toggle, and logout button.',
  },
];

function Tour({ onComplete }) {
  const { isAdmin, theme } = useAuth();
  const spotlightSteps = SPOTLIGHT_STEPS.filter(s => !s.adminOnly || isAdmin);
  const totalSteps = 1 + spotlightSteps.length; // welcome + spotlight steps

  const [currentStep, setCurrentStep] = useState(0);
  const [rect, setRect] = useState(null);

  const isWelcome = currentStep === 0;
  const spotlightIndex = currentStep - 1;
  const step = isWelcome ? null : spotlightSteps[spotlightIndex];

  const updateRect = useCallback(() => {
    if (!step) {
      setRect(null);
      return;
    }
    const el = document.querySelector(`[data-tour-target="${step.target}"]`);
    if (el) {
      setRect(el.getBoundingClientRect());
    }
  }, [step]);

  useEffect(() => {
    updateRect();
    window.addEventListener('resize', updateRect);
    window.addEventListener('scroll', updateRect, true);
    return () => {
      window.removeEventListener('resize', updateRect);
      window.removeEventListener('scroll', updateRect, true);
    };
  }, [updateRect]);

  const handleNext = () => {
    if (currentStep < totalSteps - 1) {
      setCurrentStep(currentStep + 1);
    } else {
      localStorage.setItem('ah_tourCompleted', 'true');
      onComplete();
    }
  };

  const handlePrev = () => {
    if (currentStep > 0) setCurrentStep(currentStep - 1);
  };

  const handleSkip = () => {
    localStorage.setItem('ah_tourCompleted', 'true');
    onComplete();
  };

  const logoSrc = theme === 'dark' ? '/logo-white.png' : '/logo-black.png';

  // Welcome screen (step 0)
  if (isWelcome) {
    return createPortal(
      <>
        <div className="tour-spotlight" style={{
          position: 'fixed', top: 0, left: 0, width: '100vw', height: '100vh',
          zIndex: 9998, background: 'rgba(0,0,0,0.55)',
        }} />
        <div className="tour-welcome" style={{
          position: 'fixed', top: '50%', left: '50%', transform: 'translate(-50%, -50%)',
          zIndex: 9999, width: 660,
        }}>
          <img src={logoSrc} alt="AssistantHub" className="tour-welcome-logo" />
          <p className="tour-welcome-desc">
            AssistantHub is your platform for building intelligent, document-aware AI assistants.
            Upload documents, create knowledgebases, and deploy conversational assistants that answer
            questions using your own data — all from a single dashboard.
          </p>
          <p className="tour-welcome-desc" style={{ marginTop: '0.5rem' }}>
            Let's take a quick tour to get you familiar with the interface.
          </p>
          <div className="tour-tooltip-footer" style={{ marginTop: '1.5rem' }}>
            <span className="tour-step-counter">1 of {totalSteps}</span>
            <div className="tour-tooltip-buttons">
              <button className="btn btn-ghost btn-sm" onClick={handleSkip}>Skip Tour</button>
              <button className="btn btn-primary btn-sm" onClick={handleNext}>Get Started</button>
            </div>
          </div>
        </div>
      </>,
      document.body
    );
  }

  // Spotlight steps
  if (!rect) return null;

  const padding = 8;
  const spotlightStyle = {
    position: 'fixed', top: 0, left: 0, width: '100vw', height: '100vh',
    zIndex: 9998, pointerEvents: 'auto',
  };

  const cutoutStyle = {
    position: 'fixed',
    top: rect.top - padding,
    left: rect.left - padding,
    width: rect.width + padding * 2,
    height: rect.height + padding * 2,
    borderRadius: '8px',
    boxShadow: '0 0 0 9999px rgba(0,0,0,0.55)',
    zIndex: 9998,
    pointerEvents: 'none',
  };

  // Position tooltip adjacent to cutout
  const tooltipPos = {};
  const cutoutRight = rect.left + rect.width + padding;
  const cutoutBottom = rect.top + rect.height + padding;

  if (cutoutRight + 500 < window.innerWidth) {
    tooltipPos.left = cutoutRight + 16;
    tooltipPos.top = Math.max(16, rect.top - padding);
  } else if (rect.left - padding - 500 > 0) {
    tooltipPos.left = rect.left - padding - 480 - 16;
    tooltipPos.top = Math.max(16, rect.top - padding);
  } else {
    tooltipPos.left = Math.max(16, rect.left);
    tooltipPos.top = cutoutBottom + 16;
  }

  if (tooltipPos.top + 200 > window.innerHeight) {
    tooltipPos.top = window.innerHeight - 220;
  }

  const tooltipStyle = {
    position: 'fixed', ...tooltipPos, width: 480, zIndex: 9999,
  };

  return createPortal(
    <>
      <div className="tour-spotlight" style={spotlightStyle} onClick={handleSkip} />
      <div style={cutoutStyle} />
      <div className="tour-tooltip" style={tooltipStyle}>
        <div className="tour-tooltip-title">{step.title}</div>
        <p className="tour-tooltip-desc">{step.description}</p>
        <div className="tour-tooltip-footer">
          <span className="tour-step-counter">
            {currentStep + 1} of {totalSteps}
          </span>
          <div className="tour-tooltip-buttons">
            <button className="btn btn-ghost btn-sm" onClick={handlePrev}>Previous</button>
            <button className="btn btn-ghost btn-sm" onClick={handleSkip}>Skip</button>
            <button className="btn btn-primary btn-sm" onClick={handleNext}>
              {currentStep === totalSteps - 1 ? 'Finish' : 'Next'}
            </button>
          </div>
        </div>
      </div>
    </>,
    document.body
  );
}

export default Tour;
