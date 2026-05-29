import {
	type AfterViewInit,
	Component,
	type ElementRef,
	ViewChild,
	inject,
} from "@angular/core";
import { Router } from "@angular/router";
import { AuthStateService } from "../core/auth/auth-state.service";

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
		__env?: {
			googleClientId?: string;
		};
	}
}

@Component({
	selector: "app-login-page",
	template: `
		<section class="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
			<p class="text-sm font-medium uppercase tracking-wide text-sky-700">Sign in</p>
			<h1 class="mt-2 text-3xl font-semibold">Google login</h1>
			<p class="mt-4 text-sm text-slate-600">
				Use Google Identity Services to obtain an ID token, then exchange it with the backend.
			</p>

			@if (isLoading) {
				<p class="mt-6 rounded-xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700">
					Loading Google sign-in...
				</p>
			}

			@if (errorMessage) {
				<p class="mt-6 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
					{{ errorMessage }}
				</p>
			}

			<div class="mt-6 flex flex-col gap-4">
				<div #googleButtonContainer class="min-h-10"></div>
				<p class="text-xs text-slate-500">
					The credential is kept in browser memory and forwarded to the callback route through router state,
					not the URL.
				</p>
			</div>
		</section>
	`,
})
export class LoginPageComponent implements AfterViewInit {
	private readonly router = inject(Router);
	private readonly authState = inject(AuthStateService);

	@ViewChild("googleButtonContainer", { static: true })
	private readonly googleButtonContainer?: ElementRef<HTMLDivElement>;

	isLoading = true;
	errorMessage = "";

	constructor() {
		if (this.authState.isAuthenticated()) {
			void this.router.navigateByUrl("/matches");
		}
	}

	async ngAfterViewInit(): Promise<void> {
		try {
			const clientId = this.getGoogleClientId();
			await this.loadGoogleIdentityScript();

			const googleAccounts = window.google?.accounts.id;
			const container = this.googleButtonContainer?.nativeElement;
			if (!googleAccounts || !container) {
				throw new Error("Google Identity Services failed to initialize.");
			}

			container.replaceChildren();
			googleAccounts.initialize({
				client_id: clientId,
				callback: ({ credential }) => {
					if (!credential) {
						this.errorMessage = "Google did not return an ID token.";
						return;
					}

					void this.router.navigateByUrl("/auth/callback", {
						state: { idToken: credential },
					});
				},
			});
			googleAccounts.renderButton(container, {
				type: "standard",
				theme: "outline",
				size: "large",
				shape: "pill",
				text: "continue_with",
			});

			this.isLoading = false;
		} catch (error) {
			this.isLoading = false;
			this.errorMessage =
				error instanceof Error
					? error.message
					: "Unable to load Google sign-in.";
		}
	}

	private getGoogleClientId(): string {
		const clientId = window.__env?.googleClientId;
		if (!clientId) {
			throw new Error("Google Client ID is not configured for the frontend.");
		}

		return clientId;
	}

	private async loadGoogleIdentityScript(): Promise<void> {
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
