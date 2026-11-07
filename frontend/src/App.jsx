import './App.css'

function App() {
  return (
    <main className="app">
      <section className="card">
        <span className="badge">Base del proyecto</span>
        <h1>Ascenso</h1>
        <p className="intro">
          Entorno inicial con el frontend y el backend listos para incorporar
          nuevas funcionalidades.
        </p>
        <dl className="stack">
          <div>
            <dt>Frontend</dt>
            <dd>React + Vite</dd>
          </div>
          <div>
            <dt>Backend</dt>
            <dd>.NET 10</dd>
          </div>
          <div>
            <dt>API</dt>
            <dd>OpenAPI y health checks</dd>
          </div>
          <div>
            <dt>Pruebas</dt>
            <dd>xUnit</dd>
          </div>
        </dl>
      </section>
    </main>
  )
}

export default App
