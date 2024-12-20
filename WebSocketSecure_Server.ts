// Mengimpor modul yang diperlukan dari pustaka @dgpg/chatosaurus
import { WebSocketServer, WebSocketUser, Room } from "@dgpg/chatosaurus";
import { Context, Next } from "@dgpg/chatosaurus";

// Middleware untuk logging setiap event yang terjadi
const logMiddleware = async (context: Context, next: Next) => {
  // Mencetak log dengan timestamp, nama event, dan ID pengguna
  console.log(
    `[${new Date().toISOString()}] Event: ${context.evt}, User: ${
      context.ws.id
    }`
  );
  // Melanjutkan ke middleware berikutnya atau event handler
  await next();
};

// Middleware untuk autentikasi pengguna
const authMiddleware = async (context: Context, next: Next) => {
  // Mengambil data event pertama sebagai objek yang mungkin berisi token autentikasi
  const [eventData] = context.data as { authToken?: string }[];
  // Jika token autentikasi tidak ada, kirim pesan error dan hentikan proses
  if (!eventData?.authToken) {
    context.ws.invoke("error", "Authentication required.");
    return;
  }
  // Contoh sederhana validasi token (ganti dengan logika autentikasi sesungguhnya)
  // if (eventData.authToken !== Deno.env.get("AUTH_TOKEN")) {
  //   context.ws.invoke("error", "Invalid authentication token.");
  //   return;
  // }
  // Mencetak log jika autentikasi berhasil
  console.log(`User ${context.ws.id} authenticated successfully.`);
  // Melanjutkan ke middleware berikutnya atau event handler
  await next();
};

// Membuat instance WebSocketServer dengan konfigurasi tertentu
const server = new WebSocketServer({
  hostname: "0.0.0.0",
  port: Number(Deno.env.get("PORT") || 8080),
});

// Menambahkan middleware log ke server
server.use(logMiddleware);

// Menambahkan middleware autentikasi ke server
server.use(authMiddleware);

// Menangani event saat pengguna terhubung ke server
server.on("onConnect", (user: WebSocketUser) => {
  // Mencetak log bahwa pengguna telah terhubung
  console.log(`User connected: ${user.id}`);
  // Mengirim pesan selamat datang ke pengguna yang baru terhubung
  user.invoke("welcome", `Welcome to the server! Your ID is ${user.id}`);
});

// Menangani event saat pengguna bergabung ke ruang
server.on(
  "join",
  (
    user: WebSocketUser,
    { roomId, authToken }: { roomId: string; authToken?: string }
  ) => {
    // Jika token autentikasi tidak ada, kirim pesan error dan hentikan proses
    if (!authToken) {
      user.invoke("error", "Authentication required.");
      return;
    }
    // Menambahkan pengguna ke ruang yang ditentukan
    user.join(roomId);
    // Mencetak log bahwa pengguna telah bergabung ke ruang
    console.log(`User ${user.id} joined room ${roomId}`);
  }
);

// Menangani event saat pengguna meninggalkan ruang
server.on("leave", (user: WebSocketUser, roomId: string) => {
  // Menghapus pengguna dari ruang yang ditentukan
  server.roomManager.leave(roomId, user);
  // Mencetak log bahwa pengguna telah meninggalkan ruang
  console.log(`User ${user.id} left room ${roomId}`);
});

// Menangani event saat pengguna mengirim pesan pribadi
server.on(
  "privateMessage",
  (
    user: WebSocketUser,
    {
      targetId,
      message,
      authToken,
    }: { targetId: string; message: string; authToken?: string }
  ) => {
    // Jika token autentikasi tidak ada, kirim pesan error dan hentikan proses
    if (!authToken) {
      user.invoke("error", "Authentication required.");
      return;
    }
    // Mencari pengguna target berdasarkan ID
    const target = server.clients.find((client) => client.id === targetId);
    // Jika pengguna target ditemukan, kirim pesan pribadi
    if (target) {
      target.invoke("privateMessage", { from: user.id, message });
    } else {
      // Jika pengguna target tidak ditemukan, kirim pesan error
      user.invoke("error", `User with ID ${targetId} not found.`);
    }
  }
);

// Menangani event saat pengguna mengirim pesan broadcast
server.on(
  "broadcast",
  (
    user: WebSocketUser,
    {
      message,
      authToken,
      roomId,
    }: { message: string; authToken?: string; roomId: string }
  ) => {
    // Jika token autentikasi tidak ada, kirim pesan error dan hentikan proses
    if (!authToken) {
      user.invoke("error", "Authentication required.");
      return;
    }
    // Mencari ruang berdasarkan ID ruang
    const room = server.roomManager.getRoom(roomId);
    // Jika ruang ditemukan, kirim pesan broadcast ke semua pengguna di ruang tersebut
    if (room) {
      room.invoke("message", { from: user.id, message });
    } else {
      // Jika ruang tidak ditemukan, kirim pesan error
      user.invoke("error", `Room with ID ${roomId} not found.`);
    }
  }
);

// Menangani event kesalahan yang terjadi di server
server.on("error", (err) => {
  // Mencetak log pesan kesalahan
  console.error(`[Server Error]: ${err.message}`);
  // Mencetak log stack trace kesalahan
  console.error(`Stack trace: ${err.stack}`);
});

// Memulai server WebSocket
server.start();
