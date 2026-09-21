const BASE_URL = "http://localhost:5012/api";

function getAuthHeaders() {
    const token = localStorage.getItem("fileflow_token");
    return token ? { Authorization: `Bearer ${token}` } : {};
}

function throwIfUnauthorized(response) {
    if (response.status === 401) {
        throw new Error("UNAUTHORIZED");
    }
}

// GET /api/files
export async function getFiles() {
    const response = await fetch(`${BASE_URL}/files`, {
        headers: { ...getAuthHeaders() },
    });
    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Failed to fetch files");
    return response.json();
}

// GET /api/folders
export async function getFolders() {
    const response = await fetch(`${BASE_URL}/folders`, {
        headers: { ...getAuthHeaders() },
    });
    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Failed to fetch folders");
    return response.json();
}

// GET /api/folders/{id}/contents
export async function getFolderContents(folderId) {
    const response = await fetch(`${BASE_URL}/folders/${folderId}/contents`, {
        headers: { ...getAuthHeaders() },
    });
    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Failed to fetch folder contents");
    return response.json();
}

// POST /api/files/upload
export async function uploadFile(file, folderId = null) {
    const formData = new FormData();
    formData.append("filePayload", file);
    if (folderId !== null) {
        formData.append("FolderId", folderId);
    }

    const response = await fetch(`${BASE_URL}/files/upload`, {
        method: "POST",
        headers: { ...getAuthHeaders() },
        body: formData,
    });

    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Upload failed");
    return response.json();
}

// GET /api/files/{id}/download
export async function downloadFile(id, fileName) {
    const response = await fetch(`${BASE_URL}/files/${id}/download`, {
        headers: { ...getAuthHeaders() },
    });
    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Download failed");

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);

    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
}

// DELETE /api/files/{id}
export async function deleteFile(id) {
    const response = await fetch(`${BASE_URL}/files/${id}`, {
        method: "DELETE",
        headers: { ...getAuthHeaders() },
    });
    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Delete failed");
}

// POST /api/folders
export async function createFolder(name, parentFolderId = null) {
    const response = await fetch(`${BASE_URL}/folders`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            ...getAuthHeaders(),
        },
        body: JSON.stringify({ name, parentFolderId }),
    });
    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Failed to create folder");
    return response.json();
}

// GET /api/files/search?query=
export async function searchFiles(query) {
    const response = await fetch(`${BASE_URL}/files/search?query=${encodeURIComponent(query)}`, {
        headers: { ...getAuthHeaders() },
    });
    throwIfUnauthorized(response);
    if (!response.ok) throw new Error("Search failed");
    return response.json();
}

// GET root-level folders (parentFolderId === null)
export async function getRootFolders() {
    const folders = await getFolders();
    return folders.filter((f) => f.parentFolderId === null);
}

// POST /api/auth/register
export async function register(email, password) {
    const response = await fetch(`${BASE_URL}/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
    });
    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || "Registration failed");
    }
    return response.json(); // { token, email }
}

// POST /api/auth/login
export async function login(email, password) {
    const response = await fetch(`${BASE_URL}/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
    });
    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || "Login failed");
    }
    return response.json(); // { token, email }
}