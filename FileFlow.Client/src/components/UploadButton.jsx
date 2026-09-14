import { useRef } from "react";
import { uploadFile } from "../services/api";

export default function UploadButton({ folderId, onUploaded }) {
    const inputRef = useRef(null);

    const handleFileSelected = async (event) => {
        const file = event.target.files[0];
        if (!file) return;

        await uploadFile(file, folderId);
        onUploaded(); // tells the parent to refresh the list

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
            <button onClick={() => inputRef.current.click()}>+ Upload File</button>
        </>
    );
}