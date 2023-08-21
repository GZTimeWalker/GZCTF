import { FC, forwardRef } from 'react'
import {
  ScrollAreaProps,
  ScrollArea,
  Center,
  Stack,
  createStyles,
  rem,
  UnstyledButton,
  UnstyledButtonProps,
} from '@mantine/core'

export interface SelectableItemProps<T = object> extends UnstyledButtonProps {
  onClick: () => void
  active: boolean
  disabled?: boolean
  item: T
}

interface ScrollSelectProps extends ScrollAreaProps {
  itemComponent: React.FC<any>
  emptyPlaceholder?: React.ReactNode
  items?: any[]
  customClick?: boolean
  selectedId?: number | null
  onSelectId: (item: any | null) => void
}

const useItemStyle = createStyles((theme) => ({
  root: {
    display: 'flex',
    alignItems: 'center',
    width: '100%',
    padding: `${rem(8)} ${theme.spacing.sm}`,
    userSelect: 'none',

    ...theme.fn.hover({
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
    }),

    '&[data-active]': {
      backgroundColor: theme.colors.background,
      color: theme.colors.color,
      ...theme.fn.hover({ backgroundColor: theme.colors.hover }),
    },

    '&[data-disabled]': {
      opacity: 0.4,
      pointerEvents: 'none',
    },
  },

  description: {
    display: 'block',

    '&[data-active]': {
      color: 'inherit',
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
    emptyPlaceholder,
    items,
    selectedId,
    onSelectId,
    customClick,
    ...ScrollAreaProps
  } = props

  return (
    <ScrollArea type="auto" h="calc(100vh - 110px)" {...ScrollAreaProps}>
      {!items || items.length === 0 ? (
        <Center h="100%">{emptyPlaceholder}</Center>
      ) : (
        <Stack spacing="xs" w="100%">
          {customClick
            ? items.map((item) => (
                <ItemComponent
                  key={item.id}
                  onClick={() => onSelectId(item)}
                  active={false}
                  item={item}
                />
              ))
            : items.map((item) => (
                <ItemComponent
                  key={item.id}
                  onClick={() => onSelectId(item.id)}
                  active={selectedId === item.id}
                  item={item}
                />
              ))}
        </Stack>
      )}
    </ScrollArea>
  )
}

export default ScrollSelect
