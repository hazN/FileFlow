import { useEffect, useState } from "react";
import { getFiles, getFolderContents, getRootFolders } from "../services/api";import FileList from "../components/FileList";
import FolderList from "../components/FolderList";
import UploadButton from "../components/UploadButton";
import NewFolderButton from "../components/NewFolderButton";

export default function Home() {
    const [currentFolderId, setCurrentFolderId] = useState(null);

    const [breadcrumbs, setBreadcrumbs] = useState([]);

    const [files, setFiles] = useState([]);
    const [folders, setFolders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const loadContents = async () => {
        setLoading(true);
        setError(null);
        try {
            if (currentFolderId === null) {
                const allFiles = await getFiles();
                setFiles(allFiles.filter((f) => f.folderId === null));

                const allFolders = await getRootFolders();
                setFolders(allFolders);
            } else {
                // Inside a folder: use the dedicated contents endpoint
                const data = await getFolderContents(currentFolderId);
                setFiles(data.files ?? data.Files ?? []);
                setFolders(data.subFolders ?? data.SubFolders ?? []);
            }
        } catch (err) {
            console.error(err);
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    // Re-run whenever the current folder changes
    useEffect(() => {
        loadContents();
    }, [currentFolderId]);

    const handleFolderClick = (folder) => {
        setBreadcrumbs([...breadcrumbs, { id: folder.id, name: folder.name }]);
        setCurrentFolderId(folder.id);
    };

    const handleBreadcrumbClick = (index) => {
        // Clicking a breadcrumb jumps back to that point in the trail
        if (index === -1) {
            setBreadcrumbs([]);
            setCurrentFolderId(null);
        } else {
            const newTrail = breadcrumbs.slice(0, index + 1);
            setBreadcrumbs(newTrail);
            setCurrentFolderId(newTrail[newTrail.length - 1].id);
        }
    };

    if (loading) return <div className="app"><p>Loading...</p></div>;
    if (error) return <div className="app"><p>Error: {error}</p></div>;

    return (
        <div className="app">
            <div className="app-header">
                <h1>FileFlow</h1>
            </div>

            <div className="breadcrumbs">
                <button onClick={() => handleBreadcrumbClick(-1)}>Home</button>
                {breadcrumbs.map((crumb, index) => (
                    <span key={crumb.id} style={{ display: "flex", alignItems: "center", gap: "4px" }}>
                        <span className="separator">/</span>
                        <button onClick={() => handleBreadcrumbClick(index)}>
                            {crumb.name}
                        </button>
                    </span>
                ))}
            </div>

            <div className="toolbar">
                <UploadButton folderId={currentFolderId} onUploaded={loadContents} />
                <NewFolderButton parentFolderId={currentFolderId} onCreated={loadContents} />
            </div>

            <div className="section-title">Folders</div>
            <FolderList
                folders={folders}
                onFolderClick={(id) => {
                    const folder = folders.find((f) => f.id === id);
                    handleFolderClick(folder);
                }}
            />

            <div className="section-title">Files</div>
            <FileList files={files} onFileDeleted={loadContents} />
        </div>
    );
}