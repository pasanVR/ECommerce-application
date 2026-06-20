
const Session = {
    get token() { return localStorage.getItem('jwt'); },
    get role()  { return localStorage.getItem('role'); },
    get userId(){ return localStorage.getItem('userId'); },
    isAdmin()   { return this.role === 'Admin'; },
    save(auth) {
        localStorage.setItem('jwt', auth.token);
        localStorage.setItem('role', auth.role);
        localStorage.setItem('userId', auth.userId);
    },
    clear() { localStorage.clear(); }
};

async function api(method, path, body) {
    const headers = { 'Content-Type': 'application/json' };
    if (Session.token) headers['Authorization'] = 'Bearer ' + Session.token;
    const res = await fetch('/api' + path, { method, headers, body: body ? JSON.stringify(body) : undefined });

    if (res.status === 401) { Session.clear(); location.href = '/Login'; return; }

    const text = await res.text();
    const data = text ? JSON.parse(text) : null;
    if (!res.ok) throw new Error((data && data.error) || (res.status + ' ' + res.statusText));
    return data;
}

function setMsg(id, text, ok) {
    const e = document.getElementById(id);
    if (!e) return;
    e.textContent = text || '';
    e.className = 'msg ' + (ok ? 'ok' : 'err');
}

function escapeHtml(s) {
    return (s || '').replace(/[&<>"]/g, c => ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;' }[c]));
}
