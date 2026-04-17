import { pgTable, uuid, text, timestamp, jsonb } from 'drizzle-orm/pg-core';

// ── user ──────────────────────────────────────────────────────────────────────
export const user = pgTable('user', {
  id: uuid('id').primaryKey().defaultRandom(),
  email: text('email').notNull().unique(),
  passwordHash: text('password_hash').notNull(),
  createdAt: timestamp('created_at', { withTimezone: true }).notNull().defaultNow(),
});
export type User = typeof user.$inferSelect;
export type NewUser = typeof user.$inferInsert;

// ── session ───────────────────────────────────────────────────────────────────
export const session = pgTable('session', {
  id: uuid('id').primaryKey().defaultRandom(),
  userId: uuid('user_id').notNull().references(() => user.id, { onDelete: 'cascade' }),
  expiresAt: timestamp('expires_at', { withTimezone: true }).notNull(),
  token: text('token').notNull(),
});
export type Session = typeof session.$inferSelect;
export type NewSession = typeof session.$inferInsert;

// ── save ──────────────────────────────────────────────────────────────────────
export const save = pgTable('save', {
  id: uuid('id').primaryKey().defaultRandom(),
  userId: uuid('user_id').notNull().references(() => user.id, { onDelete: 'cascade' }),
  data: jsonb('data').$type<unknown>().notNull(),
  updatedAt: timestamp('updated_at', { withTimezone: true }).notNull().defaultNow(),
});
export type Save = typeof save.$inferSelect;
export type NewSave = typeof save.$inferInsert;

// ── entitlement ───────────────────────────────────────────────────────────────
export const entitlement = pgTable('entitlement', {
  id: uuid('id').primaryKey().defaultRandom(),
  userId: uuid('user_id').notNull().references(() => user.id, { onDelete: 'cascade' }),
  tier: text('tier').notNull(), // 'free' | 'paid' — shape locked post-Step-5
  grantedAt: timestamp('granted_at', { withTimezone: true }).notNull().defaultNow(),
});
export type Entitlement = typeof entitlement.$inferSelect;
export type NewEntitlement = typeof entitlement.$inferInsert;
