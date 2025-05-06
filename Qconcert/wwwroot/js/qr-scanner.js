function startQrScanner(dotNetHelper) {
    const html5QrCode = new Html5Qrcode("reader");

    let lastScanned = null;
    let lastScanTime = 0;

    const qrCodeSuccessCallback = (decodedText, decodedResult) => {
        const now = Date.now();

        // Bỏ qua nếu quét lại cùng mã trong vòng 3 giây
        if (decodedText === lastScanned && now - lastScanTime < 3000) {
            console.log("Bỏ qua mã trùng trong thời gian ngắn:", decodedText);
            return;
        }

        lastScanned = decodedText;
        lastScanTime = now;

        console.log("Mã QR đã quét:", decodedText);

        // Gửi kết quả về Blazor
        dotNetHelper.invokeMethodAsync("OnQrCodeScanned", decodedText);

        // Hiển thị kết quả
        document.getElementById("scanResult").innerText = "Kết quả: " + decodedText;
    };

    const config = {
        fps: 10,
        qrbox: { width: 250, height: 250 }
    };

    // Liệt kê danh sách camera
    Html5Qrcode.getCameras().then(devices => {
        if (devices && devices.length) {
            console.log("Danh sách camera:", devices);

            // Tạo dropdown để chọn camera
            const cameraSelect = document.getElementById("cameraSelect");
            devices.forEach(device => {
                const option = document.createElement("option");
                option.value = device.id;
                option.text = device.label || `Camera ${cameraSelect.length + 1}`;
                cameraSelect.appendChild(option);
            });

            // Bắt đầu quét với camera đầu tiên
            cameraSelect.addEventListener("change", () => {
                const selectedCameraId = cameraSelect.value;
                html5QrCode.stop().then(() => {
                    html5QrCode.start(selectedCameraId, config, qrCodeSuccessCallback);
                });
            });

            // Khởi động camera đầu tiên
            html5QrCode.start(devices[0].id, config, qrCodeSuccessCallback)
                .then(() => {
                    console.log("Camera đã được khởi động thành công.");
                })
                .catch(err => {
                    console.error("Không thể khởi động camera: ", err);
                });
        } else {
            console.error("Không tìm thấy camera nào.");
        }
    }).catch(err => {
        console.error("Lỗi khi lấy danh sách camera:", err);
    });
}