import {
  Center,
  ElementProps,
  ScrollArea,
  ScrollAreaProps,
  Stack,
  UnstyledButton,
  UnstyledButtonProps,
} from '@mantine/core'
import cx from 'clsx'
import { FC, forwardRef } from 'react'
import classes from '@Styles/ScrollSelect.module.css'

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

export const SelectableItem = forwardRef<HTMLButtonElement, SelectableItemProps>((props, ref) => {
  const { onClick, active, children, disabled, className, ...others } = props

  return (
    <UnstyledButton
      ref={ref}
      className={cx(classes.item, className)}
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
