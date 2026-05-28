import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-login-page',
  imports: [MatButtonModule],
  template: `
    <section class="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <p class="text-sm font-medium uppercase tracking-wide text-sky-700">Auth scaffold</p>
      <h1 class="mt-2 text-3xl font-semibold">Google sign-in placeholder</h1>
      <p class="mt-4 text-sm text-slate-600">
        This page will host the Google OAuth sign-in flow. For now it only proves the standalone auth shell.
      </p>

      <div class="mt-6 flex flex-wrap gap-3">
        <button mat-flat-button color="primary" type="button">Continue with Google</button>
        <button mat-stroked-button type="button">Scaffold only</button>
      </div>
    </section>
  `
})
export class LoginPageComponent {}
