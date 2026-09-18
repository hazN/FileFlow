import { downloadFile, deleteFile } from "../services/api";

// Pick an icon based on file extension for a nicer look
function getFileIcon(fileName) {
    const ext = fileName.split(".").pop().toLowerCase();
    if (["png", "jpg", "jpeg", "gif", "svg", "webp"].includes(ext)) return "🖼️";
    if (["pdf"].includes(ext)) return "📕";
    if (["doc", "docx"].includes(ext)) return "📝";
    if (["xls", "xlsx", "csv"].includes(ext)) return "📊";
    if (["zip", "rar", "7z"].includes(ext)) return "🗜️";
    return "📄";
}

function formatSize(bytes) {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function FileList({ files, onFileDeleted }) {
    const handleDownload = (file, e) => {
        e.stopPropagation();
        downloadFile(file.id, file.name);
    };

    const handleDelete = async (file, e) => {
        e.stopPropagation();
        await deleteFile(file.id);
        onFileDeleted();
    };

    if (files.length === 0) {
        return <p className="empty-state">No files here yet.</p>;
    }

    return (
        <div className="explorer-grid">
            {files.map((file) => (
                <div key={file.id} className="explorer-item">
                    <div className="explorer-icon">{getFileIcon(file.name)}</div>
                    <div className="explorer-label">{file.name}</div>
                    <div className="explorer-meta">{formatSize(file.size)}</div>
                    <div className="file-actions">
                        <button onClick={(e) => handleDownload(file, e)}>Download</button>
                        <button onClick={(e) => handleDelete(file, e)}>Delete</button>
                    </div>
                </div>
            ))}
        </div>
    );
}