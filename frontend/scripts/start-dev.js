const { mkdirSync, writeFileSync } = require("node:fs");
const path = require("node:path");
const { spawn } = require("node:child_process");

const apiUrl = process.env.ASPIRE_API_URL ?? "http://localhost:5000";
const googleClientId = process.env.GOOGLE_CLIENT_ID ?? "";
const enableDevLogin = process.env.ENABLE_DEV_LOGIN ?? "true";
const port = process.env.PORT ?? "4200";

const generatedDir = path.join(process.cwd(), ".generated");
mkdirSync(generatedDir, { recursive: true });
writeFileSync(
  path.join(generatedDir, "env.js"),
  `window.__env = {\n  googleClientId: ${JSON.stringify(googleClientId)},\n  enableDevLogin: ${enableDevLogin === "true"}\n};\n`,
  "utf8",
);

const angularCli = path.join(
  process.cwd(),
  "node_modules",
  ".bin",
  process.platform === "win32" ? "ng.cmd" : "ng",
);

const child = spawn(
  angularCli,
  [
    "serve",
    "--host",
    "0.0.0.0",
    "--port",
    port,
    "--proxy-config",
    "proxy.conf.js",
  ],
  {
    stdio: "inherit",
    env: {
      ...process.env,
      ASPIRE_API_URL: apiUrl,
    },
  },
);

child.on("exit", (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }

  process.exit(code ?? 1);
});
