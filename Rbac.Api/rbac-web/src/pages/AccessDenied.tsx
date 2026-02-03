import { Link } from "react-router-dom";
import Card from "../ui/Card";

export default function AccessDenied() {
  return (
    <div className="status-page">
      <Card title="403 · Access denied" description="Your account does not have this permission.">
        <p>You can continue using allowed areas or request the required permission from an administrator.</p>
        <div className="status-actions">
          <Link className="btn btn--outline" to="/">
            Back to Dashboard
          </Link>
          <Link className="btn btn--ghost" to="/account">
            View My Account
          </Link>
        </div>
      </Card>
    </div>
  );
}
