// Configuración dinámica de entorno para Marea Brava
const isLocalhost = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';

// Cuando crees tu servicio en Render, colocas aquí su URL pública
const RENDER_BACKEND_URL = "https://marea-brava-api.onrender.com"; 

const BASE_URL = isLocalhost ? "" : RENDER_BACKEND_URL;

window.API_BASE = `${BASE_URL}/api`;
window.UPLOADS_BASE = `${BASE_URL}`;