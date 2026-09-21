import { useRef } from "react";
import { uploadFile } from "../services/api";

export default function UploadButton({ folderId, onUploaded, onUnauthorized }) {
    const inputRef = useRef(null);

    const handleFileSelected = async (event) => {
        const file = event.target.files[0];
        if (!file) return;

        try {
            await uploadFile(file, folderId);
            onUploaded(); // tells the parent to refresh the list
        } catch (err) {
            if (err.message === "UNAUTHORIZED") {
                onUnauthorized();
                return;
            }
            alert(err.message);
        }

        // reset the input so selecting the same file again still fires onChange
        event.target.value = "";
    };

    return (
        <>
            <input
                type="file"
                ref={inputRef}
                style={{ display: "none" }}
                onChange={handleFileSelected}
            />
            <button className="btn-primary" onClick={() => inputRef.current.click()}>+ Upload File</button>
        </>
    );
}