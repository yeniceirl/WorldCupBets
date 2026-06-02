const apiUrl = process.env.ASPIRE_API_URL ?? "http://localhost:5000";

module.exports = {
  "/api": {
    target: apiUrl,
    secure: false,
    changeOrigin: true,
  },
  "/health": {
    target: apiUrl,
    secure: false,
    changeOrigin: true,
  },
};
