import { WebSocketClient } from "@dgpg/chatosaurus";

// Membuat instance client WebSocket dengan alamat server dan opsi reconnect
const client = new WebSocketClient("wss://hisyam99-websockettest.deno.dev", {
  // Mengaktifkan fitur reconnect jika koneksi putus
  reconnect: true,
  // Mengatur waktu tunda sebelum reconnect (dalam milidetik)
  delay: 5000,
});

// Simulasi token autentikasi yang valid
const authToken = "12345";

// Mengatur event handler untuk pesan welcome dari server
client.on("welcome", (message) => {
  // Mencetak log pesan welcome dari server
  console.log("Server:", message);
});

// Mengatur event handler untuk pesan broadcast dari server
client.on("message", (data) => {
  // Mencetak log pesan broadcast dari server
  console.log(`Broadcast from ${data.from}: ${data.message}`);
});

// Mengatur event handler untuk pesan private dari server
client.on("privateMessage", (data) => {
  // Mencetak log pesan private dari server
  console.log(`Private message from ${data.from}: ${data.message}`);
});

// Mengatur event handler untuk error yang terjadi
client.on("error", (error) => {
  // Mencetak log error yang terjadi
  console.error("Error:", error);
});

// Menghubungkan client ke server
client.connect();

// Contoh interaksi dengan server
// Mengirimkan perintah join ke server setelah 2 detik
setTimeout(() => {
  // Mengirimkan perintah join ke server dengan room ID dan token autentikasi
  client.invoke("join", { roomId: "room1", authToken });
}, 2000);

// Mengirimkan perintah broadcast ke server setelah 4 detik
setTimeout(() => {
  // Mengirimkan perintah broadcast ke server dengan pesan dan token autentikasi
  client.invoke("broadcast", { message: "Halo semua dari Deno!", authToken });
}, 1000);

// Mengirimkan perintah private message ke server setelah 6 detik
setTimeout(() => {
  // Mengirimkan perintah private message ke server dengan target ID, pesan, dan token autentikasi
  client.invoke("privateMessage", { targetId: "e399764f-24c8-47af-8ba7-bcf47c01c34c", message: "Halo dari Deno!", authToken });
}, 2000);