import { Link } from 'react-router-dom';
import { Container, Card, CardBody, CardTitle } from 'shared/ui';

export function AdminPage() {
  return (
    <Container className="py-4">
      <h1 className="mb-4">Admin</h1>
      <div className="row">
        <div className="col-md-6 mb-3">
          <Card>
            <CardBody>
              <CardTitle>
                <Link to="/admin/rate">BTC/USD rate</Link>
              </CardTitle>
              <p className="mb-0 small text-muted">Текущий курс и источник (кэш / API / fallback).</p>
            </CardBody>
          </Card>
        </div>
        <div className="col-md-6 mb-3">
          <Card>
            <CardBody>
              <CardTitle>
                <Link to="/admin/skins">Skins</Link>
              </CardTitle>
              <p className="mb-0 small text-muted">Добавление, редактирование и удаление скинов.</p>
            </CardBody>
          </Card>
        </div>
        <div className="col-md-6 mb-3">
          <Card>
            <CardBody>
              <CardTitle>
                <Link to="/admin/purchases">Purchases</Link>
              </CardTitle>
              <p className="mb-0 small text-muted">История покупок с фильтрами по пользователю и дате.</p>
            </CardBody>
          </Card>
        </div>
      </div>
    </Container>
  );
}
