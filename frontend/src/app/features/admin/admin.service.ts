import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { Observable } from "rxjs";
import { loadLastPlayerSyncAtUtc, persistLastPlayerSyncAtUtc } from "./admin-player-sync-storage";
import type { AuditBalanceSummary, AuditUserSubledger, CreateUserInvitationRequest, CreateUserInvitationResult } from "./admin.models";

@Injectable({ providedIn: "root" })
export class AdminService {
	private readonly httpClient = inject(HttpClient);

	createInvitation(request: CreateUserInvitationRequest): Observable<CreateUserInvitationResult> {
		return this.httpClient.post<CreateUserInvitationResult>("/api/admin/invitations", request);
	}

	getAuditBalanceSummary(): Observable<AuditBalanceSummary> {
		return this.httpClient.get<AuditBalanceSummary>("/api/admin/audit/balances");
	}

	getAuditUserSubledger(userId: number): Observable<AuditUserSubledger> {
		return this.httpClient.get<AuditUserSubledger>(`/api/admin/audit/users/${userId}`);
	}

	getLastPlayerSyncAtUtc(): string | null {
		return loadLastPlayerSyncAtUtc();
	}

	rememberLastPlayerSyncAtUtc(syncedAtUtc: string): void {
		persistLastPlayerSyncAtUtc(syncedAtUtc);
	}
}
