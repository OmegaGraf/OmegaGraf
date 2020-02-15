import React, { useEffect } from 'react';
import './styles/App.css';
import { BrowserRouter as Router } from 'react-router-dom';
import { faReact } from '@fortawesome/free-brands-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { AppliedRoutes } from './components/Routes';
import Footer from './components/Footer';
import HeaderNav from './components/Header';
import Navbar from 'react-bootstrap/Navbar';
import { getSessionCookie } from './components/Session';
import {
  UseGlobalSession,
  getDefaults,
  UseGlobalSettings
} from './components/Global';

export default function App() {
  const [globalState, globalActions] = UseGlobalSession();
  const [globalSettings, globalSettingsActions] = UseGlobalSettings();

  useEffect(() => {
    const session = getSessionCookie();

    console.log(session);

    if (session !== null) {
      // TODO: VALIDATE API KEY HERE
      globalActions.setSession(session);
      getDefaults(session, globalSettingsActions);
    }
  }, [globalActions, globalSettingsActions]);

  return (
    <Router>
      <Navbar
        bg="dark"
        variant="dark"
        expand="lg"
        className="fixed-top header-gradient"
      >
        <Navbar.Brand href="/">
          <FontAwesomeIcon icon={faReact} className="mr-1" />
          OmegaGraf
        </Navbar.Brand>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
          <HeaderNav session={globalState} />
        </Navbar.Collapse>
      </Navbar>

      <AppliedRoutes />

      <Footer />
    </Router>
  );
}