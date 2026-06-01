import { Injectable } from "@angular/core";

declare global {
	interface Window {
		google?: {
			accounts: {
				id: {
					initialize(config: {
						client_id: string;
						callback: (response: { credential?: string }) => void;
					}): void;
					renderButton(
						parent: HTMLElement,
						options: Record<string, string | number>,
					): void;
				};
			};
		};
	}
}

@Injectable({ providedIn: "root" })
export class GoogleIdentityService {
	async renderSignInButton(
		clientId: string,
		container: HTMLElement,
		callback: (response: { credential?: string }) => void,
	): Promise<void> {
		await this.loadScript();

		const googleAccounts = window.google?.accounts.id;
		if (!googleAccounts) {
			throw new Error("Google Identity Services failed to initialize.");
		}

		container.replaceChildren();
		googleAccounts.initialize({
			client_id: clientId,
			callback,
		});
		googleAccounts.renderButton(container, {
			type: "standard",
			theme: "outline",
			size: "large",
			shape: "pill",
			text: "continue_with",
		});
	}

	private async loadScript(): Promise<void> {
		if (window.google?.accounts.id) {
			return;
		}

		await new Promise<void>((resolve, reject) => {
			const existingScript = document.querySelector<HTMLScriptElement>(
				'script[src="https://accounts.google.com/gsi/client"]',
			);
			if (existingScript) {
				existingScript.addEventListener("load", () => resolve(), {
					once: true,
				});
				existingScript.addEventListener(
					"error",
					() => reject(new Error("Failed to load Google sign-in script.")),
					{ once: true },
				);
				return;
			}

			const script = document.createElement("script");
			script.src = "https://accounts.google.com/gsi/client";
			script.async = true;
			script.defer = true;
			script.onload = () => resolve();
			script.onerror = () =>
				reject(new Error("Failed to load Google sign-in script."));
			document.head.appendChild(script);
		});
	}
}
