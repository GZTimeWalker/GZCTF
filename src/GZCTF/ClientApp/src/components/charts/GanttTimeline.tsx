import { Box, ScrollArea, Stack, Text, Title } from '@mantine/core'
import cx from 'clsx'
import dayjs, { Dayjs } from 'dayjs'
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useLanguage } from '@Utils/I18n'
import classes from '@Styles/GanttTimeline.module.css'

interface GanttTimeLineProps {
  items: GanttItem[]
}

export interface GanttItem {
  id: number
  color?: string
  textTitle: string
  title: React.ReactNode
  start: Dayjs
  end: Dayjs
}

const LabelBox = (i: number, content: string, ex: string) => (
  <Box key={i} left={i * 40} className={cx(classes.date, ex)}>
    <div className={classes.center}>
      <Text className={classes.label} span>
        {content}
      </Text>
    </div>
  </Box>
)

const TodayBox = (i: number, content: string) => (
  <Box key={i} left={i * 40} className={cx(classes.date, classes.day)}>
    <div className={cx(classes.center, classes.today)}>
      <Text className={classes.label} span>
        {content}
      </Text>
    </div>
  </Box>
)

const VIEW_WIDTH = 40 * 7 * 7
const VIEW_HEIGHT = 300
const STICKY_WIDTH = 240
const EDGE_PADDING = 10

interface MonthHeader {
  position: number
  time: Dayjs
}

interface DateData {
  start: Dayjs
  end: Dayjs
  now: Dayjs
  total: number
  duration: number

  weekends: React.ReactNode[]
  days: React.ReactNode[]
  months: React.ReactNode[]

  monthMap: MonthHeader[]
}

export const GanttTimeLine: FC<GanttTimeLineProps> = ({ items }) => {
  const viewport = useRef<HTMLDivElement>(null)
  const [scrollPosition, onScrollPositionChange] = useState({ x: 0, y: 0 })

  useEffect(() => {
    if (!viewport.current) return

    viewport.current.scrollTo({ left: viewport.current.scrollWidth / 3 })
  }, [viewport.current])

  const { t } = useTranslation()
  const { locale } = useLanguage()

  const [dateData, setDateData] = useState<DateData>({
    start: dayjs(),
    now: dayjs().startOf('h'),
    end: dayjs(),
    total: 0,
    duration: 0,
    weekends: [],
    days: [],
    months: [],
    monthMap: [],
  })

  const [currentMonth, setCurrentMonth] = useState(dayjs().locale(locale).format('SMY'))

  useEffect(() => {
    const now = dayjs().startOf('h')
    const start = now.startOf('w').subtract(3, 'w')
    const end = start.add(7, 'w').subtract(1, 's')
    const total = end.diff(start, 'd')
    const duration = end.diff(start, 's')

    const weekends: React.ReactNode[] = []
    const days: React.ReactNode[] = []
    const months: React.ReactNode[] = []
    const monthMap: MonthHeader[] = []

    monthMap.push({ position: 0, time: start })

    for (let i = 0; i <= total; i++) {
      const current = start.add(i, 'd').locale(locale)
      if (current.isSame(now, 'd')) {
        days.push(TodayBox(i, current.format('DD')))
      } else {
        days.push(LabelBox(i, current.format('DD'), classes.day))
      }
      if (current.date() === 1) {
        months.push(LabelBox(i, current.format('MMMM'), classes.month))
        monthMap.push({ position: i * 40, time: current })
      }
      if (current.day() === 6) {
        weekends.push(<Box key={i} left={i * 40} className={classes.weekend} />)
      }
    }

    monthMap.reverse()

    setDateData({
      start,
      now,
      end,
      total,
      duration,
      weekends,
      days,
      months,
      monthMap,
    })
  }, [locale])

  useEffect(() => {
    if (!dateData) return

    const map = dateData.monthMap

    for (let i = 0; i < map.length; i++) {
      if (scrollPosition.x > map[i].position) {
        const str = map[i].time.locale(locale).format('SMY')
        if (str === currentMonth) return
        setCurrentMonth(str)
        break
      }
    }
  }, [scrollPosition, locale, dateData?.monthMap])

  const nowOffset = (dateData.now.diff(dateData.start, 's') / dateData.duration) * VIEW_WIDTH + STICKY_WIDTH
  const scrollPos = scrollPosition.x + EDGE_PADDING

  return (
    <ScrollArea h={VIEW_HEIGHT} scrollbars="x" viewportRef={viewport} onScrollPositionChange={onScrollPositionChange}>
      <Box
        className={classes.view}
        __vars={{
          '--sticky-width': `${STICKY_WIDTH}px`,
          '--view-width': `${VIEW_WIDTH}px`,
          '--view-height': `${VIEW_HEIGHT}px`,
          '--now-offset': `${nowOffset}px`,
          '--edge-padding': `${EDGE_PADDING}px`,
        }}
      >
        {/* bars, position: absolute */}
        <div className={classes.background}>
          <div className={classes.weekends}>{dateData.weekends}</div>
        </div>

        {/* content rows, position: absolute  */}
        <div className={classes.dataPos}>
          {items.map((item) => {
            if (item.end.isBefore(dateData.start) || item.start.isAfter(dateData.end))
              return <div key={item.id} className={classes.dataRow} />

            const left =
              item.start < dateData.start
                ? -10
                : (item.start.diff(dateData.start, 's') / dateData.duration) * VIEW_WIDTH

            const rightOverflow = item.end > dateData.end
            const right = rightOverflow
              ? VIEW_WIDTH
              : (item.end.diff(dateData.start, 's') / dateData.duration) * VIEW_WIDTH

            const state = scrollPos < left ? 'b' : scrollPos > right ? 'a' : 'i'

            return (
              <Box
                key={item.id}
                className={classes.dataRow}
                __vars={{
                  '--left': `${left + STICKY_WIDTH}px`,
                  '--right': `${right + STICKY_WIDTH}px`,
                  '--width': `${right - left}px`,
                  '--color': item.color,
                }}
              >
                <div className={cx(classes.dataItem, classes.dataRect)} data-rov={rightOverflow || undefined}>
                  <Link className={classes.dataLink} to={`/games/${item.id}`} />
                </div>
                <div data-state={state} className={cx(classes.dataText, classes.dataRect)}>
                  <Text>{item.textTitle}</Text>
                </div>
              </Box>
            )
          })}
        </div>

        {/* headering */}
        <div className={classes.datePos}>
          <div className={classes.months}>{dateData.months}</div>
          <div className={classes.days}>{dateData.days}</div>
          <div className={classes.nowPoint} />
        </div>

        {/* current time */}
        <div className={classes.nowPos}>
          <div className={classes.nowBox}>
            <Text size="sm" fw={500}>
              {currentMonth}
            </Text>
            <div className={classes.fadeBox} />
          </div>
        </div>

        {/* title table */}
        <div className={classes.titlePos}>
          <div className={classes.titleBox}>
            <div className={classes.titleHeader}>
              <Stack justify="center" gap={4} align="center" h="100%">
                <Title order={3} size="xl">
                  {t('common.content.home.recent_games')}
                </Title>
                <Text size="sm" fw={500}>
                  <span>{dateData.start.locale(locale).format('SLL')}</span>
                  <span> - </span>
                  <span>{dateData.end.locale(locale).format('SLL')}</span>
                </Text>
              </Stack>
            </div>
            {items.map((item) => (
              <div key={item.id} className={classes.titleRow}>
                {item.title}
              </div>
            ))}
          </div>
        </div>
      </Box>
    </ScrollArea>
  )
}
