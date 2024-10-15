import {
  Box,
  Group,
  GroupProps,
  MantineColor,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { clamp } from '@mantine/hooks'
import React, { FC, useEffect, useState } from 'react'
import LogoHeader from '@Components/LogoHeader'
import classes from '@Styles/IconTabs.module.css'

interface TabProps {
  tabKey: string
  color?: MantineColor
  icon?: React.ReactNode
  label?: React.ReactNode
}

interface IconTabsProps extends GroupProps {
  position?: React.CSSProperties['justifyContent']
  tabs: TabProps[]
  grow?: boolean
  active?: number
  withIcon?: boolean
  disabled?: boolean
  aside?: React.ReactNode
  onTabChange?: (tabIndex: number, tabKey: string) => void
}

const Tab: FC<TabProps & { active: boolean; onClick?: () => void; disabled?: boolean }> = (
  props
) => {
  const { color, label, active, icon, tabKey, disabled, ...others } = props

  return (
    <Box
      key={tabKey}
      {...others}
      component="button"
      type="button"
      role="tab"
      disabled={disabled}
      __vars={{
        '--tab-active-color': color,
      }}
      data-active={active || undefined}
      className={classes.default}
    >
      <div className={classes.inner}>
        {icon && <div className={classes.icon}>{icon}</div>}
        {label && <div className={classes.label}>{label}</div>}
      </div>
    </Box>
  )
}

const IconTabs: FC<IconTabsProps> = (props) => {
  const { active, onTabChange, tabs, withIcon, aside, disabled, ...others } = props
  const [activeTab, setActiveTab] = useState(active ?? 0)
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const resolveColor = (color?: MantineColor) =>
    color ? theme.colors[theme.primaryColor][colorScheme === 'dark' ? 4 : 6] : undefined
  const current = clamp(activeTab, 0, tabs.length - 1)

  useEffect(() => {
    setActiveTab(active ?? 0)
  }, [active])

  const panes = tabs.map((tab, index) => (
    <Tab
      key={tab.tabKey}
      {...tab}
      disabled={disabled}
      color={resolveColor(tab.color)}
      active={current === index}
      onClick={() => {
        setActiveTab(index)
        onTabChange && onTabChange(index, tab.tabKey)
      }}
    />
  ))

  return (
    <Group gap={0} justify="space-between" w="100%" wrap="nowrap">
      {aside}
      {withIcon && <LogoHeader className={classes.hidable} />}
      <Group className={classes.panes} {...others}>
        {panes}
      </Group>
    </Group>
  )
}

export default IconTabs
