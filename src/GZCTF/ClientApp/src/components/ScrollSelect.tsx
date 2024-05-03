import {
  Center,
  ElementProps,
  rem,
  ScrollArea,
  ScrollAreaProps,
  Stack,
  UnstyledButton,
  UnstyledButtonProps,
} from '@mantine/core'
import { createStyles } from '@mantine/emotion'
import { FC, forwardRef } from 'react'

export interface SelectableItemProps
  extends UnstyledButtonProps,
    ElementProps<'button', keyof UnstyledButtonProps> {
  onClick: () => void
  active?: boolean
  disabled?: boolean
}

export type PropsWithItem<T = unknown, I = object> = T & { item: I }
export type SelectableItemComponent<I = object> = FC<PropsWithItem<SelectableItemProps, I>>

interface ScrollSelectProps extends ScrollAreaProps {
  itemComponent: React.FC<any>
  itemComponentProps?: any
  emptyPlaceholder?: React.ReactNode
  items?: any[]
  customClick?: boolean
  selectedId?: number | null
  onSelect?: (item: any | null) => void
}

const useItemStyle = createStyles((theme, _, u) => ({
  root: {
    display: 'flex',
    alignItems: 'center',
    padding: `${rem(8)} ${theme.spacing.sm}`,
    marginTop: '2pt',
    userSelect: 'none',
    cursor: 'pointer',
    borderRadius: theme.radius.md,

    '&:hover': {
      [u.dark]: {
        backgroundColor: theme.colors.dark[6],
      },

      [u.light]: {
        backgroundColor: theme.colors.light[2],
      },
    },

    '&[data-active]': {
      [u.dark]: {
        backgroundColor: theme.colors.dark[5],
      },

      [u.light]: {
        backgroundColor: theme.colors.light[3],
      },
    },

    '&[data-disabled]': {
      opacity: 0.4,
      pointerEvents: 'none',
    },
  },
}))

export const SelectableItem = forwardRef<HTMLButtonElement, SelectableItemProps>((props, ref) => {
  const { onClick, active, children, disabled, className, ...others } = props
  const { classes, cx } = useItemStyle()

  return (
    <UnstyledButton
      ref={ref}
      className={cx(classes.root, className)}
      data-active={active || undefined}
      onClick={onClick}
      data-disabled={disabled || undefined}
      {...others}
    >
      {children}
    </UnstyledButton>
  )
})

const ScrollSelect: FC<ScrollSelectProps> = (props) => {
  const {
    itemComponent: ItemComponent,
    itemComponentProps,
    emptyPlaceholder,
    items,
    selectedId,
    onSelect,
    ...ScrollAreaProps
  } = props

  return (
    <ScrollArea type="never" {...ScrollAreaProps}>
      {!items || items.length === 0 ? (
        <Center h="100%">{emptyPlaceholder}</Center>
      ) : (
        <Stack gap={2} w="100%">
          {items.map((item) => (
            <ItemComponent
              key={item.id}
              onClick={onSelect && (() => onSelect(item.id))}
              active={selectedId && selectedId === item.id}
              item={item}
              {...itemComponentProps}
            />
          ))}
        </Stack>
      )}
    </ScrollArea>
  )
}

export default ScrollSelect
