import { WebSocketServer, WebSocketUser, Room } from "@dgpg/chatosaurus";
import { Context, Next } from "@dgpg/chatosaurus";

/**
 * Fungsi middleware untuk mencetak log event dan user.
 * @param context Konteks permintaan WebSocket
 * @param next Fungsi next untuk melanjutkan proses
 */
const logMiddleware = async (context: Context, next: Next) => {
  // Mencetak log event dan user
  console.log(`[${new Date().toISOString()}] Event: ${context.evt}, User: ${context.ws.id}`);
  // Melanjutkan proses dengan memanggil next
  await next();
};

/**
 * Fungsi middleware untuk autentikasi user.
 * @param context Konteks permintaan WebSocket
 * @param next Fungsi next untuk melanjutkan proses
 */
const authMiddleware = async (context: Context, next: Next) => {
  // Mendapatkan data event dan token autentikasi
  const [eventData] = context.data as { authToken?: string }[];
  // Mengecek apakah token autentikasi ada
  if (!eventData?.authToken) {
    // Mengirimkan error jika autentikasi gagal
    context.ws.invoke("error", "Authentication required.");
    return;
  }
  // Mencetak log autentikasi sukses
  console.log(`User ${context.ws.id} authenticated successfully.`);
  // Melanjutkan proses dengan memanggil next
  await next();
};

// Membuat server WebSocket dengan hostname dan port
const server = new WebSocketServer({ hostname: "localhost", port: 4000 });

// Menggunakan middleware log dan autentikasi
server.use(logMiddleware);
server.use(authMiddleware);

/**
 * Event handler untuk koneksi WebSocket.
 * @param user Pengguna WebSocket yang terhubung
 */
server.on("onConnect", (user: WebSocketUser) => {
  // Mencetak log koneksi user
  console.log(`User connected: ${user.id}`);
  // Mengirimkan pesan welcome ke user
  user.invoke("welcome", `Welcome to the server! Your ID is ${user.id}`);
});

/**
 * Event handler untuk bergabung dengan room.
 * @param user Pengguna WebSocket yang bergabung dengan room
 * @param data Data event join
 */
server.on("join", (user: WebSocketUser, { roomId, authToken }: { roomId: string; authToken?: string }) => {
  // Mengecek apakah token autentikasi ada
  if (!authToken) {
    // Mengirimkan error jika autentikasi gagal
    user.invoke("error", "Authentication required.");
    return;
  }
  // Bergabung dengan room
  user.join(roomId);
  // Mencetak log bergabung dengan room
  console.log(`User ${user.id} joined room ${roomId}`);
});

/**
 * Event handler untuk meninggalkan room.
 * @param user Pengguna WebSocket yang meninggalkan room
 * @param roomId ID room yang ditinggalkan
 */
server.on("leave", (user: WebSocketUser, roomId: string) => {
  // Meninggalkan room
  server.roomManager.leave(roomId, user);
  // Mencetak log meninggalkan room
  console.log(`User ${user.id} left room ${roomId}`);
});

/**
 * Event handler untuk mengirimkan pesan private.
 * @param user Pengguna WebSocket yang mengirimkan pesan private
 * @param data Data event pesan private
 */
server.on("privateMessage", (user: WebSocketUser, { targetId, message, authToken }: { targetId: string; message: string; authToken?: string }) => {
  // Mengecek apakah token autentikasi ada
  if (!authToken) {
    // Mengirimkan error jika autentikasi gagal
    user.invoke("error", "Authentication required.");
    return;
  }
  // Mencari target pengguna
  const target = server.clients.find((client) => client.id === targetId);
  // Mengecek apakah target pengguna ditemukan
  if (target) {
    // Mengirimkan pesan private ke target pengguna
    target.invoke("privateMessage", { from: user.id, message });
  } else {
    // Mengirimkan error jika target pengguna tidak ditemukan
    user.invoke("error", `User with ID ${targetId} not found.`);
  }
});

/**
 * Event handler untuk mengirimkan pesan broadcast.
 * @param user Pengguna WebSocket yang mengirimkan pesan broadcast
 * @param data Data event pesan broadcast
 */
server.on("broadcast", (user: WebSocketUser, { message, authToken }: { message: string; authToken?: string }) => {
  // Mengecek apakah token autentikasi ada
  if (!authToken) {
    // Mengirimkan error jika autentikasi gagal
    user.invoke("error", "Authentication required.");
    return;
  }
  // Mengirimkan pesan broadcast ke semua pengguna
  server.broadcast("message", { from: user.id, message });
});

/**
 * Event handler untuk error.
 * @param err Error yang terjadi
 */
server.on("error", (err) => {
  // Mencetak log error
  console.error(`[Server Error]: ${err.message}`);
});

// Memulai server WebSocket
server.start();