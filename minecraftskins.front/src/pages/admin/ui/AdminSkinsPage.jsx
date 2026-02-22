import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Container, Row, Col, Card, CardBody, CardTitle, CardFooter, Button, Input } from 'shared/ui';
import { getSkins, createSkin, updateSkin, deleteSkin } from 'entities/skin';
import { validateSkinCreate } from 'entities/skin/model/validateSkinCreate';
import { sortSkins } from 'entities/skin/lib/skinListUtils';
import { useAuth } from 'features/auth';

function formatPrice(v) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(v);
}

export function AdminSkinsPage() {
  const { token } = useAuth();
  const [skins, setSkins] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [form, setForm] = useState({ name: '', basePriceUsd: '', isAvailable: true });
  const [editingId, setEditingId] = useState(null);
  const [saving, setSaving] = useState(false);
  const [validationErrors, setValidationErrors] = useState([]);

  const load = () => {
    if (!token) return;
    setLoading(true);
    getSkins({ availableOnly: false, take: 500 }, { token })
      .then((list) => setSkins(sortSkins(list, { sortBy: 'Date', sortOrder: 'Desc' })))
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [token]);

  const handleCreate = async (e) => {
    e.preventDefault();
    setValidationErrors([]);
    const payload = {
      name: form.name.trim(),
      basePriceUsd: Number(form.basePriceUsd),
      isAvailable: Boolean(form.isAvailable),
    };
    const { valid, errors } = validateSkinCreate(payload);
    if (!valid) {
      setValidationErrors(errors);
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await createSkin(payload, { token });
      setForm({ name: '', basePriceUsd: '', isAvailable: true });
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleUpdate = async (e, id) => {
    e.preventDefault();
    setValidationErrors([]);
    const payload = {
      name: form.name.trim(),
      basePriceUsd: Number(form.basePriceUsd),
      isAvailable: Boolean(form.isAvailable),
    };
    const { valid, errors } = validateSkinCreate(payload);
    if (!valid) {
      setValidationErrors(errors);
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await updateSkin(id, payload, { token });
      setEditingId(null);
      setForm({ name: '', basePriceUsd: '', isAvailable: true });
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Delete this skin?')) return;
    setSaving(true);
    setError(null);
    try {
      await deleteSkin(id, { token });
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const startEdit = (skin) => {
    setEditingId(skin.id);
    setForm({
      name: skin.name,
      basePriceUsd: String(skin.basePriceUsd),
      isAvailable: skin.isAvailable,
    });
  };

  return (
    <Container className="py-4">
      <p className="mb-3">
        <Link to="/admin">← Admin</Link>
      </p>
      <h1 className="mb-4">Skins (Admin)</h1>
      {error && <div className="alert alert-danger">{error}</div>}
      {validationErrors.length > 0 && (
        <div className="alert alert-warning">
          <ul className="mb-0">{validationErrors.map((msg, i) => <li key={i}>{msg}</li>)}</ul>
        </div>
      )}

      <Card className="mb-4">
        <CardBody>
          <CardTitle>Add skin</CardTitle>
          <form onSubmit={handleCreate}>
            <Input
              label="Name"
              value={form.name}
              onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
              required
            />
            <Input
              label="Base price USD"
              type="number"
              step="0.01"
              min="0"
              value={form.basePriceUsd}
              onChange={(e) => setForm((f) => ({ ...f, basePriceUsd: e.target.value }))}
              required
            />
            <label className="form-group">
              <input
                type="checkbox"
                checked={form.isAvailable}
                onChange={(e) => setForm((f) => ({ ...f, isAvailable: e.target.checked }))}
              />
              {' '}Available
            </label>
            <Button type="submit" disabled={saving}>Create</Button>
          </form>
        </CardBody>
      </Card>

      {loading ? (
        <p>Loading…</p>
      ) : (
        <Row>
          {skins.map((skin) => (
            <Col key={skin.id} size={12} className="mb-3">
              <Card>
                <CardBody>
                  {editingId === skin.id ? (
                    <form onSubmit={(e) => handleUpdate(e, skin.id)}>
                      <Input
                        label="Name"
                        value={form.name}
                        onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                        required
                      />
                      <Input
                        label="Base price USD"
                        type="number"
                        step="0.01"
                        value={form.basePriceUsd}
                        onChange={(e) => setForm((f) => ({ ...f, basePriceUsd: e.target.value }))}
                        required
                      />
                      <label>
                        <input
                          type="checkbox"
                          checked={form.isAvailable}
                          onChange={(e) => setForm((f) => ({ ...f, isAvailable: e.target.checked }))}
                        />
                        {' '}Available
                      </label>
                      <div className="mt-2">
                        <Button type="submit" disabled={saving}>Save</Button>
                        <Button type="button" variant="secondary" className="ms-2" onClick={() => { setEditingId(null); setForm({ name: '', basePriceUsd: '', isAvailable: true }); }}>
                          Cancel
                        </Button>
                      </div>
                    </form>
                  ) : (
                    <>
                      <CardTitle>{skin.name}</CardTitle>
                      <p className="mb-0">{formatPrice(skin.basePriceUsd)} · {skin.isAvailable ? 'Available' : 'Unavailable'}</p>
                    </>
                  )}
                </CardBody>
                {editingId !== skin.id && (
                  <CardFooter>
                    <Button variant="secondary" className="me-2" onClick={() => startEdit(skin)}>Edit</Button>
                    <Button variant="danger" onClick={() => handleDelete(skin.id)} disabled={saving}>Delete</Button>
                  </CardFooter>
                )}
              </Card>
            </Col>
          ))}
        </Row>
      )}
    </Container>
  );
}
