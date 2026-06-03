# AI Image Prompts — CopaCoin UI

Use these prompts to generate original artwork for the app. Do not copy FIFA mascot assets directly. The target style is mascot-inspired, playful, tournament energy, and safe for app branding.

## Asset Guidelines

- Format: `webp` or `png` with transparency when requested.
- Color mood: Canada red, Mexico green, USA navy, sky blue, gold CopaCoin accents.
- Style: friendly 3D mascot energy, premium sports UI, not childish clipart.
- Avoid: FIFA logos, World Cup trophy likeness, official mascot names, official uniforms, brand marks, country federation badges.

## 1. Leaderboard Hero Mascot

File target: `frontend/src/assets/brand/leaderboard-mascot.webp`

Size: `1024x1024`

Background: transparent

Prompt:

```text
Create an original friendly tournament mascot character for a fantasy football betting web app called CopaCoin. The mascot should feel inspired by North American World Cup energy without copying any official FIFA mascot. Use a playful animal-like hybrid with confident pose, holding a glowing golden coin marked only with "CC". Premium 3D illustration, soft studio lighting, Canada red, Mexico green, USA navy, sky blue and gold accents, transparent background, no logos, no official uniforms, no text except CC on the coin, clean silhouette, suitable for a web dashboard hero card.
```

## 2. Match Card Side Character

File target: `frontend/src/assets/brand/match-card-character.webp`

Size: `768x768`

Background: transparent

Prompt:

```text
Create an original small side character for match cards in a football prediction app. The character is an energetic fan mascot pointing toward a match ticket, with a tiny CopaCoin token and subtle stadium lights behind it. Transparent background, compact composition, readable at small size, premium 3D toy-like render, cheerful but not childish, colors: deep navy, sky blue, emerald green, warm gold. No FIFA logos, no official mascot likeness, no national federation badges, no text.
```

## 3. Admin Control Room Illustration

File target: `frontend/src/assets/brand/admin-control-room.webp`

Size: `1400x900`

Background: transparent or very dark navy

Prompt:

```text
Create a cinematic sports operations control room illustration for a football prediction app admin page. Show abstract dashboards, match result panels, glowing CopaCoin jackpot meter, and stadium floodlights in the background. Futuristic but warm, premium UI illustration, dark navy environment with gold and emerald highlights, no people with identifiable faces, no real logos, no FIFA marks, no text, enough empty space on the left for page title overlay.
```

## 4. App Background Texture

File target: `frontend/src/assets/brand/copa-bg-texture.webp`

Size: `2400x1600`

Background: full bleed

Prompt:

```text
Create a subtle abstract background texture for a football tournament web app. Use soft radial gradients, faint stadium-light arcs, tiny coin-like circles, and flowing field-line geometry. Premium modern sports design, not busy, must work behind UI cards with 10-20% opacity overlay. Color palette: light mode version with pale sky, warm ivory, soft green and gold. No logos, no text, no flags.
```

## 5. Dark Mode Background Texture

File target: `frontend/src/assets/brand/copa-bg-texture-dark.webp`

Size: `2400x1600`

Background: full bleed

Prompt:

```text
Create a subtle dark mode abstract background texture for a football tournament web app. Deep navy and midnight blue base, faint stadium-light arcs, low-opacity field-line geometry, tiny golden coin glints, soft emerald and sky-blue haze. Premium sports dashboard atmosphere, not distracting, must sit behind translucent cards. No logos, no text, no flags.
```

## 6. Empty State Character

File target: `frontend/src/assets/brand/empty-state-mascot.webp`

Size: `768x768`

Background: transparent

Prompt:

```text
Create an original empty-state mascot for a football prediction app. The character sits next to an empty match ticket and a small stack of CopaCoin tokens, looking curious and encouraging. Transparent background, rounded friendly 3D style, soft shadows, sky blue, emerald, navy and gold palette. No official sports logos, no FIFA marks, no text.
```

## Recommended Usage

- Leaderboard hero image: right side of the leaderboard header, replacing the current mascot mood link block.
- Match card side character: optional small decorative image on match cards at desktop widths only.
- Admin control room: header or right-side visual for `/admin`.
- Background textures: apply as low-opacity layered backgrounds, not as main content.
- Empty state character: use on empty `/bets`, `/leaderboard`, or no matches state.
