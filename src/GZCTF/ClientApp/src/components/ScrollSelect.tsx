import {
  Center,
  createStyles,
  rem,
  ScrollArea,
  ScrollAreaProps,
  Stack,
  UnstyledButton,
  UnstyledButtonProps,
} from '@mantine/core'
import { FC, forwardRef } from 'react'

export interface SelectableItemProps extends UnstyledButtonProps {
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

const useItemStyle = createStyles((theme) => ({
  root: {
    display: 'flex',
    alignItems: 'center',
    padding: `${rem(8)} ${theme.spacing.sm}`,
    userSelect: 'none',

    ...theme.fn.hover({
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.white[2],
    }),

    '&[data-active]': {
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.white[2],
      ...theme.fn.hover({
        backgroundColor:
          theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors.white[3],
      }),
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
        <Stack spacing={2} w="100%">
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
