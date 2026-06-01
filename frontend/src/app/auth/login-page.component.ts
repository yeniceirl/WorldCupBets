import {
	type AfterViewInit,
	Component,
	DestroyRef,
	type ElementRef,
	ViewChild,
	inject,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Router } from "@angular/router";
import { AuthService } from "../core/auth/auth.service";
import { GoogleIdentityService } from "../core/auth/google-identity.service";
import { AuthStateService } from "../core/auth/auth-state.service";

declare global {
	interface Window {
		__env?: {
			googleClientId?: string;
			enableDevLogin?: boolean;
		};
	}
}

@Component({
	selector: "app-login-page",
	standalone: true,
	template: `
		<section class="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm" data-testid="login-page">
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
				<div #googleButtonContainer class="min-h-10" data-testid="google-login-container"></div>
				@if (isDevLoginEnabled) {
					<button
						type="button"
						class="rounded-xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-800 transition hover:border-sky-400 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-60"
						(click)="signInWithDevLogin()"
						[disabled]="isDevLoginLoading"
						data-testid="dev-login-button"
					>
						{{ isDevLoginLoading ? "Signing in..." : "Use development login" }}
					</button>
				}
				<p class="text-xs text-slate-500">
					The credential is kept in browser memory and forwarded to the callback route through router state,
					not the URL.
				</p>
			</div>
		</section>
	`,
})
export class LoginPageComponent implements AfterViewInit {
	private readonly destroyRef = inject(DestroyRef);
	private readonly router = inject(Router);
	private readonly authService = inject(AuthService);
	private readonly googleIdentityService = inject(GoogleIdentityService);
	private readonly authState = inject(AuthStateService);

	@ViewChild("googleButtonContainer", { static: true })
	private readonly googleButtonContainer?: ElementRef<HTMLDivElement>;

	isLoading = true;
	errorMessage = "";
	isDevLoginEnabled = window.__env?.enableDevLogin === true;
	isDevLoginLoading = false;

	constructor() {
		if (this.authState.isAuthenticated()) {
			void this.router.navigateByUrl("/matches");
		}
	}

	signInWithDevLogin(): void {
		this.errorMessage = "";
		this.isDevLoginLoading = true;

		this.authService.devLogin()
			.pipe(takeUntilDestroyed(this.destroyRef))
			.subscribe({
				next: () => {
					void this.router.navigateByUrl("/matches");
				},
				error: (error: { error?: { error?: string; detail?: string } }) => {
					this.isDevLoginLoading = false;
					this.errorMessage =
						error.error?.error ??
						error.error?.detail ??
						"Development login failed.";
				},
			});
	}

	async ngAfterViewInit(): Promise<void> {
		const clientId = this.getGoogleClientId();
		if (!clientId) {
			this.isLoading = false;
			if (!this.isDevLoginEnabled) {
				this.errorMessage = "Google Client ID is not configured for the frontend.";
			}
			return;
		}

		try {
			const container = this.googleButtonContainer?.nativeElement;
			if (!container) {
				throw new Error("Google Identity Services failed to initialize.");
			}

			await this.googleIdentityService.renderSignInButton(
				clientId,
				container,
				({ credential }) => {
					if (!credential) {
						this.errorMessage = "Google did not return an ID token.";
						return;
					}

					void this.router.navigateByUrl("/auth/callback", {
						state: { idToken: credential },
					});
				},
			);

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
		return window.__env?.googleClientId?.trim() ?? "";
	}
}
