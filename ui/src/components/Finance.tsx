import { useState } from 'react';
import FinanceDeposits from './FinanceDeposits';
import FinanceInvestments from './FinanceInvestments';
import FinanceLedger from './FinanceLedger';
import FinanceOverviewPane from './FinanceOverviewPane';
import FinanceRecurring from './FinanceRecurring';
import FinanceTargets from './FinanceTargets';

type Pane = 'overview' | 'recurring' | 'ledger' | 'investments' | 'deposits' | 'targets';

const PANES: { id: Pane; label: string }[] = [
  { id: 'overview', label: 'Overview' },
  { id: 'recurring', label: 'Recurring' },
  { id: 'ledger', label: 'Ledger' },
  { id: 'investments', label: 'Investments' },
  { id: 'deposits', label: 'Deposits' },
  { id: 'targets', label: 'Targets' },
];

export default function Finance({ onUnauthorized }: { onUnauthorized: () => void }) {
  const [pane, setPane] = useState<Pane>('overview');

  return (
    <>
      <nav className="sub">
        {PANES.map((item) => (
          <button
            key={item.id}
            type="button"
            className={item.id === pane ? 'tab selected' : 'tab'}
            onClick={() => setPane(item.id)}
          >
            {item.label}
          </button>
        ))}
      </nav>

      {/* Remounting on every switch, as the top-level nav does: logging an
          expense here has to move the numbers on Overview. */}
      {pane === 'overview' && <FinanceOverviewPane onUnauthorized={onUnauthorized} />}
      {pane === 'recurring' && <FinanceRecurring onUnauthorized={onUnauthorized} />}
      {pane === 'ledger' && <FinanceLedger onUnauthorized={onUnauthorized} />}
      {pane === 'investments' && <FinanceInvestments onUnauthorized={onUnauthorized} />}
      {pane === 'deposits' && <FinanceDeposits onUnauthorized={onUnauthorized} />}
      {pane === 'targets' && <FinanceTargets onUnauthorized={onUnauthorized} />}
    </>
  );
}
