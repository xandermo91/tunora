import { Component, type ReactNode } from 'react';

interface Props { children: ReactNode; }
interface State { error: Error | null; }

export default class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  render() {
    if (this.state.error) {
      return (
        <div className="min-h-screen bg-sp-black flex items-center justify-center p-8">
          <div className="bg-sp-gray rounded-xl p-8 max-w-md w-full text-center">
            <p className="text-2xl mb-4">⚠</p>
            <h2 className="text-sp-white text-lg font-bold mb-2">Something went wrong</h2>
            <p className="text-sp-subtext text-sm mb-6">{this.state.error.message}</p>
            <button
              onClick={() => window.location.reload()}
              className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-5 py-2 rounded-full text-sm transition-colors"
            >
              Reload
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
