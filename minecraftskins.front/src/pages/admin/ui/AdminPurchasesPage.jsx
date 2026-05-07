import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Container, Button, Input } from 'shared/ui';
import { useAuth } from 'features/auth';
import { getPurchases } from 'entities/purchase';

export function AdminPurchasesPage() {
  const { token } = useAuth();
  const [buyerUserName, setBuyerUserName] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [items, setItems] = useState([]);

  const load = async () => {
    if (!token) return;
    const data = await getPurchases(
      { buyerUserName: buyerUserName || undefined, from: from || undefined, to: to || undefined, take: 200 },
      { token }
    );
    setItems(data);
  };

  useEffect(() => {
    load();
  }, [token]);

  return (
    <Container className="py-4">
      <p><Link to="/admin">← Admin</Link></p>
      <h1>Purchase history</h1>
      <div className="d-flex gap-2 align-items-end mb-3">
        <Input label="Username contains" value={buyerUserName} onChange={(e) => setBuyerUserName(e.target.value)} />
        <Input label="From" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        <Input label="To" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        <Button onClick={load}>Apply</Button>
      </div>
      <div className="list-group">
        {items.map((p) => (
          <div key={p.id} className="list-group-item">
            <strong>{p.skin?.name ?? p.skinId}</strong> - ${Number(p.priceUsdFinal).toFixed(2)}
            <div className="small text-muted">{p.buyerId} · {new Date(p.purchasedAtUtc).toLocaleString()}</div>
          </div>
        ))}
      </div>
    </Container>
  );
}
