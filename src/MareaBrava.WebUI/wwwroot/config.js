// Configuración dinámica de entorno para Marea Brava
const isLocalhost = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';

// Replace this URL with the public Render service URL before deploying the frontend.
const RENDER_BACKEND_URL = "https://mareabrava-1.onrender.com";

const BASE_URL = isLocalhost ? "" : RENDER_BACKEND_URL;

window.API_BASE = `${BASE_URL}/api`;
window.UPLOADS_BASE = `${BASE_URL}`;

window.apiUrl = function (path) {
	if (!path || !path.startsWith('/api')) return path;
	return `${window.API_BASE}${path.substring(4)}`;
};

window.mediaUrl = function (path) {
	if (!path || path.startsWith('http')) return path;
	return `${window.UPLOADS_BASE}${path.startsWith('/') ? path : `/${path}`}`;
};

const nativeFetch = window.fetch.bind(window);
window.fetch = function (input, init = {}) {
	const requestUrl = typeof input === 'string' ? window.apiUrl(input) : input;
	return nativeFetch(requestUrl, { ...init, credentials: 'include' });
};