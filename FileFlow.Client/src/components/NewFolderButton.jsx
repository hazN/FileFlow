import { useState } from "react";
import { createFolder } from "../services/api";

export default function NewFolderButton({ parentFolderId, onCreated }) {
    const [showInput, setShowInput] = useState(false);
    const [name, setName] = useState("");

    const handleCreate = async () => {
        if (!name.trim()) return;

        try {
            await createFolder(name, parentFolderId);
            setName("");
            setShowInput(false);
            onCreated(); // tells the parent to refresh
        } catch (err) {
            alert(err.message); // e.g. shows your backend's validation error
        }
    };

    if (!showInput) {
        return <button onClick={() => setShowInput(true)}>+ New Folder</button>;
    }

    return (
        <span>
            <input
                autoFocus
                value={name}
                onChange={(e) => setName(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && handleCreate()}
                placeholder="Folder name"
            />
            <button onClick={handleCreate}>Create</button>
            <button className="btn-primary" onClick={() => setShowInput(true)}>+ New Folder</button>
        </span>
    );
}