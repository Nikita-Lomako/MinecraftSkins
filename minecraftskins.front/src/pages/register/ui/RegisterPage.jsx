import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Container, Row, Col, Input, Button } from 'shared/ui';
import { register, login, useAuth } from 'features/auth';

export function RegisterPage() {
  const navigate = useNavigate();
  const { signIn } = useAuth();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await register({ userName, password });
      const res = await login({ userName, password });
      signIn(res.token, { id: res.userName, name: res.userName });
      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container className="py-5">
      <Row className="justify-content-center">
        <Col size={6}>
          <h1 className="text-center mb-4">Register</h1>
          {error && (
            <div className="alert alert-danger" role="alert">
              {error}
            </div>
          )}
          <form onSubmit={handleSubmit}>
            <Input
              label="Username"
              name="userName"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              required
              autoComplete="username"
            />
            <Input
              label="Password"
              name="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="new-password"
            />
            <Button type="submit" disabled={loading}>
              {loading ? 'Registering…' : 'Register'}
            </Button>
          </form>
          <p className="mt-3 text-center">
            Already have an account? <Link to="/login">Sign In</Link>
          </p>
        </Col>
      </Row>
    </Container>
  );
}
