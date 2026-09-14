import { downloadFile, deleteFile } from "../services/api";

export default function FileList({ files, onFileDeleted }) {
    const handleDownload = (file) => {
        downloadFile(file.id, file.name);
    };

    const handleDelete = async (file) => {
        await deleteFile(file.id);
        onFileDeleted(); // tells the parent to refresh the list
    };

    return (
        <ul>
            {files.map((file) => (
                <li key={file.id}>
                    📄 {file.name} ({file.size} bytes)
                    <button onClick={() => handleDownload(file)}>Download</button>
                    <button onClick={() => handleDelete(file)}>Delete</button>
                </li>
            ))}
        </ul>
    );
}