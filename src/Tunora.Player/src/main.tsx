import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

// StrictMode is intentionally omitted: the kiosk player maintains a single
// persistent SignalR connection, and StrictMode's double-mount behaviour
// would stop the connection mid-negotiate in development.
createRoot(document.getElementById('root')!).render(<App />)
