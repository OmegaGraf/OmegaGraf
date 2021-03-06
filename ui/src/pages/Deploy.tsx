import React from 'react';
import { Container, Breadcrumb } from 'react-bootstrap';
import 'rc-steps/assets/index.css';
import RunDeploy from '../components/deployment/run/RunDeploy';
import { Link } from 'react-router-dom';

export default function Deploy() {
  const params = new URLSearchParams(window.location.search);
  const ref = params.get('ref');

  return (
    <main role="main" className="container">
      <Breadcrumb>
        <Breadcrumb.Item>
          <Link to="/">Home</Link>
        </Breadcrumb.Item>
        <Breadcrumb.Item>
          <Link to={'/form/' + ref}>Form</Link>
        </Breadcrumb.Item>
        <Breadcrumb.Item active>Deploy</Breadcrumb.Item>
      </Breadcrumb>
      <br />
      <h1 className="home-title text-center">Deploy</h1>
      <h4 className="home-subtitle text-center">Create your environment.</h4>
      <br />
      <hr />
      <br />
      <Container>
        <RunDeploy />
      </Container>
    </main>
  );
}
