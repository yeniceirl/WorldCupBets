export function formatCopaCoin(amountCc: number): string {
	return amountCc.toLocaleString(undefined, {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	});
}
