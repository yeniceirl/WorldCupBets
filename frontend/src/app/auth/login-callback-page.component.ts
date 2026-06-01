import { Component, DestroyRef, OnInit, inject } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Router } from "@angular/router";
import { AuthService } from "../core/auth/auth.service";
import { AuthStateService } from "../core/auth/auth-state.service";

@Component({
	selector: "app-login-callback-page",
	standalone: true,
	template: `
		<section class="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm" data-testid="login-callback-page">
			<p class="text-sm font-medium uppercase tracking-wide text-sky-700">Google callback</p>
			<h1 class="mt-2 text-3xl font-semibold">Signing you in</h1>
			<p class="mt-4 text-sm text-slate-600">
				This screen exchanges the Google ID token with the backend and stores the JWT through the auth service.
			</p>

			@if (isLoading) {
				<div class="mt-6 rounded-xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700">
					Exchanging Google token...
				</div>
			}

			@if (errorMessage) {
				<div class="mt-6 space-y-4">
					<p class="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
						{{ errorMessage }}
					</p>
					<div class="flex flex-wrap gap-3">
						<button
							type="button"
							class="rounded-xl bg-sky-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-sky-700"
							(click)="goBack()"
						>
							Back to login
						</button>
					</div>
				</div>
			}
		</section>
	`,
})
export class LoginCallbackPageComponent implements OnInit {
	private readonly destroyRef = inject(DestroyRef);
	private readonly router = inject(Router);
	private readonly authService = inject(AuthService);
	private readonly authState = inject(AuthStateService);

	isLoading = true;
	errorMessage = "";

	ngOnInit(): void {
		if (this.authState.isAuthenticated()) {
			void this.router.navigateByUrl("/matches");
			return;
		}

		const idToken = history.state?.idToken?.trim?.() ?? "";
		if (!idToken) {
			this.isLoading = false;
			this.errorMessage =
				"Missing Google ID token. Start again from the login page.";
			return;
		}

		this.authService.exchangeGoogleToken(idToken)
			.pipe(takeUntilDestroyed(this.destroyRef))
			.subscribe({
				next: () => {
					void this.router.navigateByUrl("/matches");
				},
				error: (error: { error?: { error?: string; detail?: string } }) => {
					this.isLoading = false;
					this.errorMessage =
						error.error?.error ??
						error.error?.detail ??
						"The Google token exchange failed. Please try again.";
				},
			});
	}

	goBack(): void {
		void this.router.navigateByUrl("/auth/login");
	}
}
