import { Component } from "@angular/core";

@Component({
	selector: "app-login-callback-page",
	template: `
    <section class="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <p class="text-sm font-medium uppercase tracking-wide text-sky-700">Auth scaffold</p>
      <h1 class="mt-2 text-3xl font-semibold">Login callback placeholder</h1>
      <p class="mt-4 text-sm text-slate-600">
        This route will process the OAuth callback and exchange the Google token with the backend.
      </p>
    </section>
  `,
})
export class LoginCallbackPageComponent {}
