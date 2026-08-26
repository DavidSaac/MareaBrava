// Configuración dinámica de entorno para Marea Brava
// El frontend y la API se sirven desde el mismo origen (Azure App Service), por lo que se usan rutas relativas.
const BASE_URL = "";

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