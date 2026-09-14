export default function FolderList({ folders, onFolderClick }) {
    return (
        <ul>
            {folders.map((folder) => (
                <li key={folder.id}>
                    <button onClick={() => onFolderClick(folder.id)}>
                        📁 {folder.name}
                    </button>
                </li>
            ))}
        </ul>
    );
}