export default function FolderList({ folders, onFolderClick }) {
    if (folders.length === 0) {
        return <p className="empty-state">No folders here yet.</p>;
    }

    return (
        <div className="explorer-grid">
            {folders.map((folder) => (
                <div
                    key={folder.id}
                    className="explorer-item"
                    onClick={() => onFolderClick(folder.id)}
                >
                    <div className="explorer-icon">📁</div>
                    <div className="explorer-label">{folder.name}</div>
                </div>
            ))}
        </div>
    );
}