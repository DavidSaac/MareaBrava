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
window.fetch = async function (input, init = {}) {
	const requestUrl = typeof input === 'string' ? window.apiUrl(input) : input;
	const response = await nativeFetch(requestUrl, { ...init, credentials: 'include' });
	const requestUrlValue = typeof requestUrl === 'string' ? requestUrl : requestUrl.url ?? requestUrl.toString();
	const pathname = new URL(requestUrlValue, window.location.origin).pathname;
	if (response.status === 401 && pathname.startsWith('/api/') && !pathname.endsWith('/auth/login') && !pathname.endsWith('/auth/logout')) {
		window.dispatchEvent(new Event('mb:session-expired'));
	}
	return response;
};