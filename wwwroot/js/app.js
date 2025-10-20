// File download helper
window.downloadFile = function (fileName, contentType, data) {
    // Convert byte array to blob
    const blob = new Blob([new Uint8Array(data)], { type: contentType });

    // Create download link
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;

    // Trigger download
    document.body.appendChild(link);
    link.click();

    // Cleanup
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};

// Print PDF helper - opens PDF in new window for printing
window.printPdf = function (data) {
    // Convert byte array to blob
    const blob = new Blob([new Uint8Array(data)], { type: 'application/pdf' });
    
    // Create object URL for the blob
    const url = window.URL.createObjectURL(blob);
    
    // Open in new window
    const printWindow = window.open(url, '_blank');
    
    if (printWindow) {
        // Wait for PDF to load, then trigger print dialog
        printWindow.onload = function() {
            printWindow.print();
            // Clean up URL after a delay to allow printing
            setTimeout(() => {
                window.URL.revokeObjectURL(url);
            }, 1000);
        };
    } else {
        // Fallback if popup blocked - download instead
        const link = document.createElement('a');
        link.href = url;
        link.download = 'invoice.pdf';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        alert('Pop-up blocked. PDF downloaded instead. Please open it to print.');
    }
};