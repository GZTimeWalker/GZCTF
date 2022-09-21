import Autoplay from 'embla-carousel-autoplay'
import { FC, useRef } from 'react'
import { Carousel, CarouselProps } from '@mantine/carousel'
import { BasicGameInfoModel } from '@Api'
import RecentGameSlide from './RecentGameSlide'

interface RecentGameCarouselProps extends CarouselProps {
  games: BasicGameInfoModel[]
}

const RecentGameCarousel: FC<RecentGameCarouselProps> = ({ games, ...props }) => {
  const autoplay = useRef(Autoplay({ delay: 5000 }))

  return (
    <Carousel
      sx={{ width: '100%' }}
      mx="auto"
      loop
      withIndicators
      withControls={false}
      height={200}
      plugins={[autoplay.current]}
      onMouseEnter={autoplay.current.stop}
      onMouseLeave={autoplay.current.reset}
      {...props}
    >
      {games.map((game) => (
        <Carousel.Slide key={game.id}>
          <RecentGameSlide game={game} />
        </Carousel.Slide>
      ))}
    </Carousel>
  )
}

export default RecentGameCarousel
