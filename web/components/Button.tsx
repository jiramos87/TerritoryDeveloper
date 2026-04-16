import type { ComponentPropsWithoutRef, ReactNode } from 'react'

export type ButtonVariant = 'primary' | 'secondary' | 'ghost'
export type ButtonSize = 'sm' | 'md' | 'lg'

export type ButtonProps = {
  variant?: ButtonVariant
  size?: ButtonSize
  href?: string
  disabled?: boolean
  className?: string
  children: ReactNode
} & Omit<ComponentPropsWithoutRef<'button'>, 'disabled' | 'className' | 'children'>

const VARIANT_CLASS: Record<ButtonVariant, string> = {
  primary:   'bg-bg-status-progress text-text-status-progress-fg',
  secondary: 'bg-bg-panel text-text-primary border border-text-muted/40',
  ghost:     'bg-transparent text-text-muted hover:text-text-primary',
}

const SIZE_CLASS: Record<ButtonSize, string> = {
  sm: 'px-2 py-1 text-xs',
  md: 'px-3 py-1.5 text-sm',
  lg: 'px-4 py-2 text-base',
}

const BASE_CLASS = 'inline-flex items-center justify-center rounded font-mono transition-colors'
const DISABLED_CLASS = 'opacity-50 cursor-not-allowed pointer-events-none'

export function Button({
  variant = 'primary',
  size = 'md',
  href,
  disabled = false,
  className,
  children,
  ...rest
}: ButtonProps): ReactNode {
  const variantCls = VARIANT_CLASS[variant]
  const sizeCls = SIZE_CLASS[size]
  const disabledCls = disabled ? DISABLED_CLASS : ''
  const combined = `${BASE_CLASS} ${variantCls} ${sizeCls} ${disabledCls} ${className ?? ''}`.trim()

  if (href != null) {
    return (
      <a href={href} className={combined}>
        {children}
      </a>
    )
  }

  return (
    <button type="button" disabled={disabled} className={combined} {...rest}>
      {children}
    </button>
  )
}
