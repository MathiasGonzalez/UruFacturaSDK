import { useState } from 'react';
import { useAuth } from '../services/AuthContext.jsx';
import { emitirCfe, generarXml } from '../services/api.js';
import './EmitirCfe.css';

const TIPOS_CFE = [
  { value: 101, label: 'e-Ticket (101)' },
  { value: 102, label: 'Nota Crédito e-Ticket (102)' },
  { value: 103, label: 'Nota Débito e-Ticket (103)' },
  { value: 111, label: 'e-Factura (111)' },
  { value: 112, label: 'Nota Crédito e-Factura (112)' },
  { value: 113, label: 'Nota Débito e-Factura (113)' },
  { value: 121, label: 'e-Factura Exportación (121)' },
  { value: 122, label: 'NC Exportación (122)' },
  { value: 123, label: 'ND Exportación (123)' },
  { value: 131, label: 'e-Remito Despachante (131)' },
  { value: 151, label: 'e-Resguardo (151)' },
  { value: 181, label: 'e-Remito (181)' },
  { value: 182, label: 'NC e-Remito (182)' },
];

export default function EmitirCfe() {
  const { tenant } = useAuth();
  const [tipo, setTipo] = useState(101);
  const [numero, setNumero] = useState(1);
  const [item, setItem] = useState({ nombre: '', cantidad: 1, precio: 0 });
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const buildRequest = () => ({
    Tipo: tipo,
    Numero: numero,
    Serie: null,
    FormaPago: 1,
    Moneda: 0,
    Receptor: null,
    Detalle: [
      {
        NroLinea: 1,
        NombreItem: item.nombre || 'Producto',
        Cantidad: item.cantidad,
        PrecioUnitario: item.precio,
        IndFactIva: 3,
      },
    ],
  });

  const handleEmitir = async () => {
    setError('');
    setResult(null);
    setLoading(true);
    try {
      const resp = await emitirCfe(tenant?.tenantId, buildRequest());
      setResult(resp);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleXml = async () => {
    setError('');
    setResult(null);
    setLoading(true);
    try {
      const xml = await generarXml(tenant?.tenantId, buildRequest());
      setResult({ xml });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="emitir-page">
      <h1>Emitir CFE</h1>

      <div className="card form-section">
        <div className="form-grid">
          <div className="field">
            <label>Tipo de CFE</label>
            <select value={tipo} onChange={(e) => setTipo(Number(e.target.value))}>
              {TIPOS_CFE.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Número</label>
            <input
              type="number"
              min="1"
              value={numero}
              onChange={(e) => setNumero(Number(e.target.value))}
            />
          </div>
        </div>

        <h3>Línea de detalle</h3>
        <div className="form-grid three-cols">
          <div className="field">
            <label>Nombre ítem</label>
            <input
              type="text"
              placeholder="Servicio de consultoría"
              value={item.nombre}
              onChange={(e) => setItem({ ...item, nombre: e.target.value })}
            />
          </div>
          <div className="field">
            <label>Cantidad</label>
            <input
              type="number"
              min="1"
              value={item.cantidad}
              onChange={(e) => setItem({ ...item, cantidad: Number(e.target.value) })}
            />
          </div>
          <div className="field">
            <label>Precio unitario</label>
            <input
              type="number"
              min="0"
              step="0.01"
              value={item.precio}
              onChange={(e) => setItem({ ...item, precio: Number(e.target.value) })}
            />
          </div>
        </div>

        <div className="actions">
          <button className="primary" onClick={handleEmitir} disabled={loading}>
            {loading ? 'Enviando...' : 'Enviar a DGI'}
          </button>
          <button className="secondary" onClick={handleXml} disabled={loading}>
            Solo generar XML
          </button>
        </div>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {result && (
        <div className="card result-section">
          <h3>Resultado</h3>
          <pre>{result.xml || JSON.stringify(result, null, 2)}</pre>
        </div>
      )}
    </div>
  );
}
